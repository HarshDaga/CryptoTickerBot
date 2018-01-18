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
using static System.Console;
using Timer = System.Timers.Timer;

// ReSharper disable FunctionNeverReturns
#pragma warning disable 4014

namespace CryptoTickerBot
{
	public class Program
	{
		private static readonly string[] Scopes = {SheetsService.Scope.Spreadsheets};
		private const string ApplicationName = "Crypto Ticker Bot";
		private const string SheetName = "Tickers";
		private static string sheetId;
		private static SheetsService service;

		private static readonly Dictionary<CryptoExchange, string> SheetsRanges = new Dictionary<CryptoExchange, string>
		{
			[CryptoExchange.BitBay] = "A3:D6",
			[CryptoExchange.Koinex] = "A12:D15",
			[CryptoExchange.Binance] = "A20:D23",
			//[CryptoExchange.CoinDelta] = "A29:D32",
			[CryptoExchange.Coinbase] = "A37:D40",
			[CryptoExchange.Kraken] = "A29:D32",
		};

		private static readonly ConcurrentQueue<CryptoExchange> PendingUpdates =
			new ConcurrentQueue<CryptoExchange> ( );

		public static void Main ( string[] args )
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

			foreach ( var exchange in exchanges.Values )
			{
				exchange.Changed += ( e, coin ) =>
				{
					if ( !PendingUpdates.Contains ( e.Id ) )
						PendingUpdates.Enqueue ( e.Id );
				};
				exchange.Changed += ( e, coin ) => WriteLine ( $"{e.Name,-10} {e[coin.Symbol]}" );
				try
				{
					Task.Run ( ( ) => exchange.GetExchangeData ( CancellationToken.None ) );
				}
				catch ( Exception e )
				{
					WriteLine ( e );
					throw;
				}
			}

			sheetId = File.ReadAllText ( "sheet.id" );
			CreateSheetsService ( );

			StartAutoSheetsUpdater ( exchanges );

			Thread.Sleep ( int.MaxValue );
		}

		private static void StartAutoSheetsUpdater ( IReadOnlyDictionary<CryptoExchange, CryptoExchangeBase> exchanges )
		{
			Task.Run ( ( ) =>
			{
				Thread.Sleep ( 10000 );
				var updateTimer = new Timer ( 1000 );
				updateTimer.Elapsed += ( sender, eventArgs ) =>
				{
					while ( PendingUpdates.TryDequeue ( out var id ) )
					{
						if ( !exchanges[id].IsComplete )
						{
							WriteLine ( $"Sheets not updated for {id}. Only {exchanges[id].ExchangeData.Count} coins updated." );
							WriteLine ( $"{exchanges[id].ExchangeData.Keys.Join ( ", " )}." );
							continue;
						}

						var range = SheetsRanges[id];
						UpdateSheet ( range, exchanges[id] );
						WriteLine ( $"Updated Sheets for {id}" );
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
				ApplicationName = ApplicationName
			} );
		}

		private static void UpdateSheet (
			string range,
			CryptoExchangeBase exchange
		)
		{
			var valueRange = new ValueRange
			{
				Values = exchange.ToSheetRows ( ),
				Range = $"{SheetName}!{range}"
			};
			var requestBody = new BatchUpdateValuesRequest
			{
				ValueInputOption = "USER_ENTERED",
				Data = new List<ValueRange> {valueRange}
			};

			var request = service.Spreadsheets.Values.BatchUpdate ( requestBody, sheetId );

			request.ExecuteAsync ( );
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
				WriteLine ( "Credential file saved to: " + credPath );
			}

			return credential;
		}
	}
}