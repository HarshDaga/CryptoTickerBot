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
using CryptoTickerBot.Helpers;
using NLog;
using Timer = System.Timers.Timer;

namespace CryptoTickerBot.Core
{
	public partial class Bot : IDisposable
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public readonly Dictionary<CryptoExchangeId, CryptoExchangeBase> Exchanges =
			new Dictionary<CryptoExchangeId, CryptoExchangeBase>
			{
				[CryptoExchangeId.Koinex]    = new KoinexExchange ( ),
				[CryptoExchangeId.BitBay]    = new BitBayExchange ( ),
				[CryptoExchangeId.Binance]   = new BinanceExchange ( ),
				[CryptoExchangeId.CoinDelta] = new CoinDeltaExchange ( ),
				[CryptoExchangeId.Coinbase]  = new CoinbaseExchange ( ),
				[CryptoExchangeId.Kraken]    = new KrakenExchange ( )
			};

		private CancellationTokenSource cts;
		private Timer fiatMonitor;

		private ImmutableHashSet<CryptoExchangeId> pendingUpdates =
			ImmutableHashSet<CryptoExchangeId>.Empty;

		public CryptoCompareTable CompareTable { get; } =
			new CryptoCompareTable ( );

		public bool IsRunning { get; private set; }
		public bool IsInitialized { get; private set; }

		public static List<Data.Domain.CryptoCoin> SupportedCoins
		{
			get
			{
				List<Data.Domain.CryptoCoin> result;
				using ( var unit = new UnitOfWork ( ) )
				{
					result = unit.Coins.GetAll ( ).ToList ( );
					unit.Complete ( );
				}

				return result;
			}
		}

		public void Dispose ( )
		{
			Dispose ( true );
			GC.SuppressFinalize ( this );
		}

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
				exchange.Next    += UpdateExchangeLastUpdateInDb;
				exchange.Changed += ( e, coin ) => Logger.Debug ( $"{e.Name,-10} {e[coin.Id]}" );
				exchange.Changed += StoreCoinValueInDb;
				exchange.Changed += UpdateExchangeLastChangeInDb;
				exchange.Changed += ( ex, coin ) => pendingUpdates = pendingUpdates.Add ( ex.Id );

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

		private static void StoreCoinValueInDb ( CryptoExchangeBase exchange, CryptoCoin coin )
		{
			using ( var unit = new UnitOfWork ( ) )
			{
				unit.CoinValues.AddCoinValue ( coin.Id, exchange.Id, coin.LowestAsk, coin.HighestBid, coin.Time );
				unit.Complete ( );
			}
		}

		private static void UpdateExchangeLastChangeInDb ( CryptoExchangeBase exchange, CryptoCoin coin )
		{
			using ( var unit = new UnitOfWork ( ) )
			{
				unit.Exchanges.UpdateExchange ( exchange.Id, lastChange: DateTime.UtcNow );
				unit.Complete ( );
			}
		}

		private static void UpdateExchangeLastUpdateInDb ( CryptoExchangeBase exchange, CryptoCoin coin )
		{
			using ( var unit = new UnitOfWork ( ) )
			{
				unit.Exchanges.UpdateExchange ( exchange.Id, lastUpdate: DateTime.UtcNow );
				unit.Complete ( );
			}
		}

		private void Dispose ( bool disposing )
		{
			if ( !disposing ) return;

			cts?.Dispose ( );
			fiatMonitor?.Dispose ( );
			service?.Dispose ( );
		}

		~Bot ( )
		{
			Dispose ( false );
		}
	}
}