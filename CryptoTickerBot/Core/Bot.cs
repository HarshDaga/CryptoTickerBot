using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Exchanges;
using CryptoTickerBot.Extensions;
using CryptoTickerBot.Helpers;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Timer = System.Timers.Timer;

namespace CryptoTickerBot.Core
{
	public static class Bot
	{
		private static readonly string[] Scopes = {SheetsService.Scope.Spreadsheets};
		private static SheetsService service;

		private static readonly ConcurrentQueue<CryptoExchange> PendingUpdates =
			new ConcurrentQueue<CryptoExchange> ( );

		public static Task Start ( string[] args )
		{
			return Task.Run ( async ( ) =>
				{
					FiatConverter.StartMonitor ( );

					var exchanges = new Dictionary<CryptoExchange, CryptoExchangeBase>
					{
						[CryptoExchange.Koinex] = new KoinexExchange ( ),
						[CryptoExchange.BitBay] = new BitBayExchange ( ),
						[CryptoExchange.Binance] = new BinanceExchange ( ),
						//[CryptoExchange.CoinDelta] = new CoinDeltaExchange ( ),
						[CryptoExchange.Coinbase] = new CoinbaseExchange ( ),
						[CryptoExchange.Kraken] = new KrakenExchange ( ),
					};

					InitExchanges ( exchanges );

					CreateSheetsService ( );

					StartAutoSheetsUpdater ( exchanges );

					await Task.Delay ( int.MaxValue );
				}
			);
		}

		private static void InitExchanges ( Dictionary<CryptoExchange, CryptoExchangeBase> exchanges )
		{
			foreach ( var exchange in exchanges.Values )
			{
				exchange.Changed += ( e, coin ) =>
				{
					if ( !PendingUpdates.Contains ( e.Id ) )
						PendingUpdates.Enqueue ( e.Id );
				};
				exchange.Changed += ( e, coin ) => Console.WriteLine ( $"{e.Name,-10} {e[coin.Symbol]}" );
				try
				{
					Task.Run ( ( ) => exchange.StartMonitor ( ) );
				}
				catch ( Exception e )
				{
					Console.WriteLine ( e );
					throw;
				}
			}
		}

		private static void StartAutoSheetsUpdater ( IReadOnlyDictionary<CryptoExchange, CryptoExchangeBase> exchanges )
		{
			Task.Run ( ( ) =>
			{
				Thread.Sleep ( 10000 );
				var updateTimer = new Timer ( 1000 );
				updateTimer.Elapsed += async ( sender, eventArgs ) =>
				{
					try
					{
						var valueRanges = new List<ValueRange> ( );
						while ( PendingUpdates.TryDequeue ( out var id ) )
						{
							var exchange = exchanges[id];
							if ( !exchange.IsComplete )
							{
								Console.WriteLine ( $"Sheets not updated for {id}. Only {exchange.ExchangeData.Count} coins updated." );
								Console.WriteLine ( $"{exchange.ExchangeData.Keys.Join ( ", " )}." );
								continue;
							}

							var range = Settings.Instance.SheetsRanges[id];
							valueRanges.Add ( new ValueRange
							{
								Values = exchange.ToSheetRows ( ),
								Range = $"{Settings.Instance.SheetName}!{range}"
							} );
							Console.WriteLine ( $"Updated Sheets for {id}" );
						}
						await UpdateSheet ( valueRanges );
					}
					catch ( Exception e )
					{
						Console.WriteLine ( e );
					}
				};
				updateTimer.Start ( );
			} );
		}

		private static void CreateSheetsService ( )
		{
			var credential = GetCredentials ( );

			service = new SheetsService ( new BaseClientService.Initializer
			{
				HttpClientInitializer = credential,
				ApplicationName = Settings.Instance.ApplicationName
			} );
		}

		private static async Task UpdateSheet ( IList<ValueRange> valueRanges )
		{
			try
			{
				var requestBody = new BatchUpdateValuesRequest
				{
					ValueInputOption = "USER_ENTERED",
					Data = valueRanges
				};

				var request = service.Spreadsheets.Values.BatchUpdate ( requestBody, Settings.Instance.SheetId );

				await request.ExecuteAsync ( );
			}
			catch ( Google.GoogleApiException e )
			{
				if ( e.Error.Code == 429 )
					Console.WriteLine ( "ERROR: Too many Google Api requests. Cooling down." );
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
				Console.WriteLine ( "Credential file saved to: " + credPath );
			}

			return credential;
		}
	}
}