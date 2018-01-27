﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Exchanges;
using CryptoTickerBot.Extensions;
using CryptoTickerBot.Helpers;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using NLog;
using Timer = System.Timers.Timer;

namespace CryptoTickerBot.Core
{
	public class Bot
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private static readonly string[] Scopes = {SheetsService.Scope.Spreadsheets};

		public readonly Dictionary<CryptoExchange, CryptoExchangeBase> Exchanges =
			new Dictionary<CryptoExchange, CryptoExchangeBase>
			{
				[CryptoExchange.Koinex] = new KoinexExchange ( ),
				[CryptoExchange.BitBay] = new BitBayExchange ( ),
				[CryptoExchange.Binance] = new BinanceExchange ( ),
				//[CryptoExchange.CoinDelta] = new CoinDeltaExchange ( ),
				[CryptoExchange.Coinbase] = new CoinbaseExchange ( ),
				[CryptoExchange.Kraken] = new KrakenExchange ( )
			};

		public readonly Dictionary<CryptoExchange, CryptoExchangeObserver> Observers =
			new Dictionary<CryptoExchange, CryptoExchangeObserver> ( );

		private readonly ConcurrentQueue<CryptoExchange> pendingUpdates =
			new ConcurrentQueue<CryptoExchange> ( );

		private SheetsService service;

		public CryptoCompareTable CompareTable { get; } =
			new CryptoCompareTable ( );

		public Task Start ( )
		{
			return Task.Run ( async ( ) =>
				{
					FiatConverter.StartMonitor ( );

					InitExchanges ( );

					CreateSheetsService ( );

					StartAutoSheetsUpdater ( );

					await Task.Delay ( int.MaxValue );
				}
			);
		}

		private void InitExchanges ( )
		{
			foreach ( var exchange in Exchanges.Values )
			{
				Observers[exchange.Id] = new CryptoExchangeObserver ( exchange );
				exchange.Changed += ( e, coin ) =>
				{
					if ( !pendingUpdates.Contains ( e.Id ) )
						pendingUpdates.Enqueue ( e.Id );
				};
				var observer = Observers[exchange.Id];
				observer.Next += ( e, coin ) => Logger.Debug ( $"{e.Name,-10} {e[coin.Symbol]}" );
				exchange.Subscribe ( observer );
				CompareTable.AddExchange ( exchange );
				try
				{
					Task.Run ( ( ) => exchange.StartMonitor ( ) );
				}
				catch ( Exception e )
				{
					Logger.Error ( e );
					throw;
				}
			}
		}

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
						( sender as Timer )?.Start ( );
					}
				};
				updateTimer.Start ( );
			} );
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

				await request.ExecuteAsync ( );
			}
			catch ( Exception e )
			{
				if ( e is GoogleApiException gae && gae.Error.Code == 429 )
				{
					Logger.Error ( gae, "Too many Google Api requests. Cooling down." );
					await Task.Delay ( 5000 );
				}
				else
				{
					Logger.Error ( e );
				}
			}
		}

		private UserCredential GetCredentials ( )
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