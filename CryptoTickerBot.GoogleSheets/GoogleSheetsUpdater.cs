using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Extensions;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using JetBrains.Annotations;
using NLog;
using Timer = System.Timers.Timer;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace CryptoTickerBot.GoogleSheets
{
	public class GoogleSheetsUpdater : IDisposable
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private static readonly string[] Scopes = {SheetsService.Scope.Spreadsheets};

		private ImmutableHashSet<CryptoExchangeId> pendingUpdates =
			ImmutableHashSet<CryptoExchangeId>.Empty;

		public SheetsService Service { get; }
		public CryptoTickerBotCore Ctb { get; }
		public CancellationTokenSource Cts { get; }

		public int UpdateFrequency { get; set; }
		public string ApplicationName { get; }
		public string SheetName { get; }
		public string SheetId { get; }
		public IDictionary<CryptoExchangeId, string> SheetsRanges { get; }

		private GoogleSheetsUpdater (
			[NotNull] SheetsService service,
			[NotNull] CryptoTickerBotCore ctb,
			[NotNull] string applicationName,
			[NotNull] string sheetName,
			[NotNull] string sheetId,
			[NotNull] IDictionary<CryptoExchangeId, string> sheetsRanges,
			int updateFrequency
		)
		{
			Service         = service;
			Ctb             = ctb;
			Cts             = Ctb.Cts;
			ApplicationName = applicationName;
			SheetName       = sheetName;
			SheetId         = sheetId;
			SheetsRanges    = sheetsRanges;
			UpdateFrequency = updateFrequency;

			foreach ( var exchange in Ctb.Exchanges.Values )
				exchange.Changed += ( ex, coin ) => pendingUpdates = pendingUpdates.Add ( ex.Id );

			Start ( );
		}

		public void Dispose ( ) => Service?.Dispose ( );

		private static UserCredential GetCredentials ( )
		{
			UserCredential credential;

			using ( var stream =
				new FileStream ( "client_secret.json", FileMode.Open, FileAccess.Read ) )
			{
				var credPath = Environment.GetFolderPath (
					Environment.SpecialFolder.Personal );
				credPath = Path.Combine ( credPath, ".credentials/sheets.googleapis.com-dotnet-quickstart.json" );

				credential = GoogleWebAuthorizationBroker.AuthorizeAsync (
					GoogleClientSecrets.Load ( stream ).Secrets,
					Scopes,
					"user",
					CancellationToken.None,
					new FileDataStore ( credPath, true ) ).Result;
				Logger.Info ( "Credential file saved to: " + credPath );
			}

			return credential;
		}

		public static GoogleSheetsUpdater Build (
			[NotNull] CryptoTickerBotCore ctb,
			[NotNull] string applicationName,
			[NotNull] string sheetName,
			[NotNull] string sheetId,
			[NotNull] IDictionary<CryptoExchangeId, string> sheetsRanges,
			int updateFrequency = 2000
		)
		{
			Logger.Info ( $"Building Google Sheets Service for {applicationName}" );
			var credential = GetCredentials ( );

			var service = new SheetsService ( new BaseClientService.Initializer
			{
				HttpClientInitializer = credential,
				ApplicationName       = applicationName
			} );

			return new GoogleSheetsUpdater (
				service, ctb, applicationName, sheetName, sheetId, sheetsRanges, updateFrequency
			);
		}

		private async Task UpdateSheet ( IList<ValueRange> valueRanges )
		{
			try
			{
				var requestBody = new BatchUpdateValuesRequest
				{
					ValueInputOption = "USER_ENTERED",
					Data             = valueRanges
				};

				var request = Service.Spreadsheets.Values.BatchUpdate (
					requestBody,
					SheetId
				);

				await request.ExecuteAsync ( Cts.Token );
			}
			catch ( TaskCanceledException tce )
			{
				if ( !Cts.IsCancellationRequested )
					Logger.Warn ( tce );
			}
			catch ( Exception e )
			{
				if ( e is GoogleApiException gae && gae.Error.Code == 429 )
				{
					Logger.Error ( gae, "Too many Google Api requests. Cooling down." );
					await Task.Delay ( 5000, Cts.Token );
				}
				else
				{
					Logger.Error ( e );
				}
			}
		}

		private void Start ( )
		{
			if ( Service is null )
				return;

			Task.Run ( ( ) =>
			{
				Thread.Sleep ( 10000 );
				var updateTimer = new Timer ( UpdateFrequency )
				{
					Enabled   = true,
					AutoReset = false
				};
				updateTimer.Elapsed += async ( sender, eventArgs ) =>
				{
					if ( Cts.IsCancellationRequested )
						return;

					try
					{
						var valueRanges = GetValueRangesToUpdate ( );
						await UpdateSheet ( valueRanges );
					}
					catch ( Exception e )
					{
						Logger.Error ( e );
					}
					finally
					{
						if ( !Cts.IsCancellationRequested )
							( sender as Timer )?.Start ( );
					}
				};
				updateTimer.Start ( );
			}, Cts.Token );
		}

		private List<ValueRange> GetValueRangesToUpdate ( )
		{
			var valueRanges = new List<ValueRange> ( );
			while ( pendingUpdates.Count > 0 )
			{
				var id = pendingUpdates.First ( );
				pendingUpdates = pendingUpdates.Remove ( id );
				var exchange = Ctb.Exchanges[id];
				if ( !exchange.IsComplete )
				{
					Logger.Warn (
						$"Sheets not updated for {id}. Only {exchange.ExchangeData.Count} coins updated." +
						$" {exchange.ExchangeData.Keys.Join ( ", " )}." );
					continue;
				}

				if ( !SheetsRanges.ContainsKey ( id ) )
					continue;

				var range = SheetsRanges[id];
				valueRanges.Add ( new ValueRange
				{
					Values = exchange.ToSheetRows ( ),
					Range  = $"{SheetName}!{range}"
				} );
				Logger.Debug ( $"Updated Sheets for {id}" );
			}

			return valueRanges;
		}
	}
}