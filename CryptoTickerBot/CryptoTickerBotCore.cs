using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Persistence;
using CryptoTickerBot.Exchanges;
using CryptoTickerBot.Exchanges.Core;
using CryptoTickerBot.Helpers;
using JetBrains.Annotations;
using NLog;
using Timer = System.Timers.Timer;

namespace CryptoTickerBot
{
	public class CryptoTickerBotCore : IDisposable
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public readonly IDictionary<CryptoExchangeId, CryptoExchangeBase> Exchanges =
			new ConcurrentDictionary<CryptoExchangeId, CryptoExchangeBase>
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

		public CancellationTokenSource Cts { get; private set; }

		public CryptoCompareTable CompareTable { get; } =
			new CryptoCompareTable ( );

		public bool IsRunning { get; private set; }
		public bool IsInitialized { get; private set; }

		public static List<Data.Domain.CryptoCoin> SupportedCoins =>
			UnitOfWork.Get ( u => u.Coins.GetAll ( ).ToList ( ) );

		public CryptoExchangeBase this [ CryptoExchangeId exchangeId ] =>
			Exchanges[exchangeId];

		public void Dispose ( )
		{
			Dispose ( true );
			GC.SuppressFinalize ( this );
		}

		[UsedImplicitly]
		public event Action<CryptoTickerBotCore> Close;

		public static CryptoTickerBotCore CreateAndStart ( [NotNull] CancellationTokenSource cts )
		{
			var bot = new CryptoTickerBotCore ( );
			bot.Start ( cts );
			return bot;
		}

		public static CryptoTickerBotCore CreateAndStart ( ) =>
			CreateAndStart ( new CancellationTokenSource ( ) );

		public void Start ( [NotNull] CancellationTokenSource cts )
		{
			Logger.Info ( "Starting Bot" );

			Cts         = cts;
			IsRunning   = true;
			fiatMonitor = FiatConverter.StartMonitor ( );

			InitExchanges ( );

			Task.Run ( async ( ) =>
			{
				await Task.Delay ( int.MaxValue, Cts.Token );

				IsRunning = false;
				Close?.Invoke ( this );
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
			UnitOfWork.Do ( u => u.CoinValues.Add ( coin.ToCryptoCoinValue ( exchange.Id ) ) );

		private static void UpdateExchangeLastChangeInDb ( CryptoExchangeBase exchange, CryptoCoin coin ) =>
			UnitOfWork.Do ( u => u.Exchanges.UpdateExchange ( exchange.Id, lastChange: DateTime.UtcNow ) );

		private static void UpdateExchangeLastUpdateInDb ( CryptoExchangeBase exchange, CryptoCoin coin ) =>
			UnitOfWork.Do ( u => u.Exchanges.UpdateExchange ( exchange.Id, lastUpdate: DateTime.UtcNow ) );

		private void Dispose ( bool disposing )
		{
			if ( !disposing ) return;

			Cts?.Dispose ( );
			fiatMonitor?.Dispose ( );
		}

		~CryptoTickerBotCore ( )
		{
			Dispose ( false );
		}
	}
}