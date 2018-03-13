using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Persistence;
using CryptoTickerBot.Exchanges;
using CryptoTickerBot.Exchanges.Core;
using CryptoTickerBot.Extensions;
using CryptoTickerBot.Helpers;
using Google.Apis.Sheets.v4.Data;
using JetBrains.Annotations;
using NLog;
using Timer = System.Timers.Timer;

namespace CryptoTickerBot.Core
{
	public class CryptoTickerBot : IDisposable
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public readonly Dictionary<CryptoExchangeId, CryptoExchangeBase> Exchanges =
			new Dictionary<CryptoExchangeId, CryptoExchangeBase>
			{
				[CryptoExchangeId.Binance]   = new BinanceExchange ( ),
				[CryptoExchangeId.BitBay]    = new BitBayExchange ( ),
				[CryptoExchangeId.Bitstamp]  = new BitstampExchange ( ),
				[CryptoExchangeId.Coinbase]  = new CoinbaseExchange ( ),
				[CryptoExchangeId.CoinDelta] = new CoinDeltaExchange ( ),
				[CryptoExchangeId.Koinex]    = new KoinexExchange ( ),
				[CryptoExchangeId.Kraken]    = new KrakenExchange ( ),
				[CryptoExchangeId.Zebpay]    = new ZebpayExchange ( )
			};

		private Timer fiatMonitor;

		private ImmutableHashSet<CryptoExchangeId> pendingUpdates =
			ImmutableHashSet<CryptoExchangeId>.Empty;

		public CancellationTokenSource Cts { get; private set; }

		public GoogleSheetsService Service { get; }

		public CryptoCompareTable CompareTable { get; } =
			new CryptoCompareTable ( );

		public IDictionary<CryptoExchangeId, string> SheetsRanges { get; }

		public bool IsRunning { get; private set; }
		public bool IsInitialized { get; private set; }

		public static List<Data.Domain.CryptoCoin> SupportedCoins =>
			UnitOfWork.Get ( u => u.Coins.GetAll ( ).ToList ( ) );

		public CryptoTickerBot (
			[CanBeNull] GoogleSheetsService service,
			[CanBeNull] IDictionary<CryptoExchangeId, string> sheetsRanges
		)
		{
			Service      = service;
			SheetsRanges = sheetsRanges;
		}

		public void Dispose ( )
		{
			Dispose ( true );
			GC.SuppressFinalize ( this );
		}

		public static CryptoTickerBot CreateAndStart (
			[NotNull] CancellationTokenSource cts,
			[NotNull] string applicationName,
			[NotNull] string sheetName,
			[NotNull] string sheetId,
			[NotNull] IDictionary<CryptoExchangeId, string> sheetsRanges
		)
		{
			var service = GoogleSheetsService.Build (
				cts,
				applicationName,
				sheetName,
				sheetId
			);
			var bot = new CryptoTickerBot ( service, sheetsRanges );
			bot.Start ( cts );

			return bot;
		}

		public static CryptoTickerBot CreateAndStart ( [NotNull] CancellationTokenSource cts )
		{
			var bot = new CryptoTickerBot ( null, null );
			bot.Start ( cts );
			return bot;
		}

		public void Start ( [NotNull] CancellationTokenSource cts )
		{
			Logger.Info ( "Starting Bot" );

			Cts         = cts;
			IsRunning   = true;
			fiatMonitor = FiatConverter.StartMonitor ( );

			InitExchanges ( );
			StartAutoSheetsUpdater ( );

			Task.Run ( async ( ) =>
			{
				await Task.Delay ( int.MaxValue, Cts.Token );

				IsRunning = false;
			}, Cts.Token );
		}

		public void Stop ( )
		{
			Logger.Info ( "Stopping Bot" );
			fiatMonitor?.Stop ( );
			Cts?.Cancel ( );
		}

		private void InitExchanges ( )
		{
			foreach ( var exchange in Exchanges.Values )
			{
				exchange.Next    += UpdateExchangeLastUpdateInDb;
				exchange.Changed += ( e, coin ) => Logger.Debug ( $"{e.Name,-10} {e[coin.Id]}" );
				exchange.Changed += StoreCoinValueInDb;
				exchange.Changed += UpdateExchangeLastChangeInDb;
				exchange.Changed += ( ex, coin ) => pendingUpdates = pendingUpdates.Add ( ex.Id );

				CompareTable.AddExchange ( exchange );

				try
				{
					Task.Run ( ( ) => exchange.StartMonitor ( Cts.Token ), Cts.Token );
				}
				catch ( Exception e )
				{
					Logger.Error ( e );
					throw;
				}
			}

			IsInitialized = true;
		}

		private static void StoreCoinValueInDb ( CryptoExchangeBase exchange, CryptoCoin coin ) =>
			UnitOfWork.Do ( u => u.CoinValues.AddCoinValue (
				                coin.Id, exchange.Id,
				                coin.LowestAsk, coin.HighestBid, coin.Time
			                ) );

		private static void UpdateExchangeLastChangeInDb ( CryptoExchangeBase exchange, CryptoCoin coin ) =>
			UnitOfWork.Do ( u => u.Exchanges.UpdateExchange ( exchange.Id, lastChange: DateTime.UtcNow ) );

		private static void UpdateExchangeLastUpdateInDb ( CryptoExchangeBase exchange, CryptoCoin coin ) =>
			UnitOfWork.Do ( u => u.Exchanges.UpdateExchange ( exchange.Id, lastUpdate: DateTime.UtcNow ) );

		private void Dispose ( bool disposing )
		{
			if ( !disposing ) return;

			Cts?.Dispose ( );
			fiatMonitor?.Dispose ( );
			Service?.Dispose ( );
		}

		private void StartAutoSheetsUpdater ( )
		{
			if ( Service is null )
				return;

			Task.Run ( ( ) =>
			{
				Thread.Sleep ( 10000 );
				var updateTimer = new Timer ( 1500 )
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
						await Service.UpdateSheet ( valueRanges );
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
				var exchange = Exchanges[id];
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
					Range  = $"{Service.SheetName}!{range}"
				} );
				Logger.Debug ( $"Updated Sheets for {id}" );
			}

			return valueRanges;
		}

		~CryptoTickerBot ( )
		{
			Dispose ( false );
		}
	}
}