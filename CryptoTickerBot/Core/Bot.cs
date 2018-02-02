using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Exchanges;
using CryptoTickerBot.Helpers;
using NLog;
using Timer = System.Timers.Timer;

namespace CryptoTickerBot.Core
{
	public partial class Bot
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public readonly Dictionary<CryptoExchange, CryptoExchangeBase> Exchanges =
			new Dictionary<CryptoExchange, CryptoExchangeBase>
			{
				[CryptoExchange.Koinex]    = new KoinexExchange ( ),
				[CryptoExchange.BitBay]    = new BitBayExchange ( ),
				[CryptoExchange.Binance]   = new BinanceExchange ( ),
				[CryptoExchange.CoinDelta] = new CoinDeltaExchange ( ),
				[CryptoExchange.Coinbase]  = new CoinbaseExchange ( ),
				[CryptoExchange.Kraken]    = new KrakenExchange ( )
			};

		public readonly Dictionary<CryptoExchange, CryptoExchangeObserver> Observers =
			new Dictionary<CryptoExchange, CryptoExchangeObserver> ( );

		private readonly ConcurrentQueue<CryptoExchange> pendingUpdates =
			new ConcurrentQueue<CryptoExchange> ( );

		private CancellationTokenSource cts;
		private Timer fiatMonitor;

		public CryptoCompareTable CompareTable { get; } =
			new CryptoCompareTable ( );

		public bool IsRunning { get; private set; }
		public bool IsInitialized { get; private set; }

		public void Start ( )
		{
			IsRunning = true;
			cts       = new CancellationTokenSource ( );
			Task.Run ( async ( ) =>
			{
				fiatMonitor = FiatConverter.StartMonitor ( );

				InitExchanges ( );

				IsInitialized = true;

				CreateSheetsService ( );

				StartAutoSheetsUpdater ( );

				await Task.Delay ( int.MaxValue, cts.Token );

				IsRunning = false;
			}, cts.Token );
		}

		public void Stop ( )
		{
			fiatMonitor.Stop ( );
			cts.Cancel ( );
		}

		private void InitExchanges ( )
		{
			foreach ( var exchange in Exchanges.Values )
			{
				exchange.ClearObservers ( );
				Observers[exchange.Id] =  new CryptoExchangeObserver ( exchange );
				exchange.Changed       += ( e, coin ) =>
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
					Task.Run ( ( ) => exchange.StartMonitor ( cts.Token ), cts.Token );
				}
				catch ( Exception e )
				{
					Logger.Error ( e );
					throw;
				}
			}
		}
	}
}