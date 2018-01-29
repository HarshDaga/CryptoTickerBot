using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Extensions;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Timer = System.Timers.Timer;

namespace CryptoTickerBot.Core
{
	public partial class Bot
	{
		private static readonly string[] Scopes = {SheetsService.Scope.Spreadsheets};
		private SheetsService service;

		private void StartAutoSheetsUpdater ( )
		{
			Task.Run ( ( ) =>
			{
				Thread.Sleep ( 10000 );
				var updateTimer = new Timer ( 1000 )
				{
					Enabled = true,
					AutoReset = false
				};
				updateTimer.Elapsed += async ( sender, eventArgs ) =>
				{
					if ( cts.IsCancellationRequested )
						return;

					try
					{
						var valueRanges = new List<ValueRange> ( );
						while ( pendingUpdates.TryDequeue ( out var id ) )
						{
							var exchange = Exchanges[id];
							if ( !exchange.IsComplete )
							{
								Logger.Warn (
									$"Sheets not updated for {id}. Only {exchange.ExchangeData.Count} coins updated." +
									$" {exchange.ExchangeData.Keys.Join ( ", " )}." );
								continue;
							}

							if ( !Settings.Instance.SheetsRanges.ContainsKey ( id ) )
								continue;

							var range = Settings.Instance.SheetsRanges[id];
							valueRanges.Add ( new ValueRange
							{
								Values = exchange.ToSheetRows ( ),
								Range = $"{Settings.Instance.SheetName}!{range}"
							} );
							Logger.Info ( $"Updated Sheets for {id}" );
						}

						await UpdateSheet ( valueRanges );
					}
					catch ( Exception e )
					{
						Logger.Error ( e );
					}
					finally
					{
						if ( !cts.IsCancellationRequested )
							( sender as Timer )?.Start ( );
					}
				};
				updateTimer.Start ( );
			}, cts.Token );
		}

		private void CreateSheetsService ( )
		{
			var credential = GetCredentials ( );

			service = new SheetsService ( new BaseClientService.Initializer
			{
				HttpClientInitializer = credential,
				ApplicationName = Settings.Instance.ApplicationName
			} );
		}

		private async Task UpdateSheet ( IList<ValueRange> valueRanges )
		{
			try
			{
				var requestBody = new BatchUpdateValuesRequest
				{
					ValueInputOption = "USER_ENTERED",
					Data = valueRanges
				};

				var request = service.Spreadsheets.Values.BatchUpdate ( requestBody, Settings.Instance.SheetId );

				await request.ExecuteAsync ( cts.Token );
			}
			catch ( Exception e )
			{
				if ( e is GoogleApiException gae && gae.Error.Code == 429 )
				{
					Logger.Error ( gae, "Too many Google Api requests. Cooling down." );
					await Task.Delay ( 5000, cts.Token );
				}
				else if ( !( e is TaskCanceledException ) )
				{
					Logger.Error ( e );
				}
			}
		}

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
	}
}