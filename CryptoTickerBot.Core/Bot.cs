using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Configs;
using CryptoTickerBot.Core.Exchanges;
using CryptoTickerBot.Core.Helpers;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Enums;
using JetBrains.Annotations;
using NLog;
using static CryptoTickerBot.Enums.CryptoExchangeId;

namespace CryptoTickerBot.Core
{
	public class Bot : IDisposable
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public static readonly IDictionary<CryptoExchangeId, ICryptoExchange> AllExchanges;
		public IDictionary<CryptoExchangeId, ICryptoExchange> Exchanges { get; private set; }

		public CancellationTokenSource Cts { get; private set; }

		public bool IsRunning { get; private set; }
		public bool IsInitialized { get; private set; }

		public ICryptoExchange this [ CryptoExchangeId index ]
		{
			get
			{
				Exchanges.TryGetValue ( index, out var exchange );
				return exchange;
			}
		}

		static Bot ( )
		{
			AllExchanges = ImmutableDictionary<CryptoExchangeId, ICryptoExchange>.Empty
				.AddRange ( new Dictionary<CryptoExchangeId, ICryptoExchange>
				{
					[Binance]   = new BinanceExchange ( ),
					[Bitstamp]  = new BitstampExchange ( ),
					[Coinbase]  = new CoinbaseExchange ( ),
					[CoinDelta] = new CoinDeltaExchange ( ),
					[Koinex]    = new KoinexExchange ( ),
					[Kraken]    = new KrakenExchange ( ),
					[Zebpay]    = new ZebpayExchange ( )
				} );
		}

		public void Dispose ( )
		{
			Cts?.Dispose ( );
		}

		[UsedImplicitly]
		public event TerminateDelegate Terminate;

		[UsedImplicitly]
		public event OnUpdateDelegate Changed;

		[UsedImplicitly]
		public event OnUpdateDelegate Next;

		public async Task StartAsync ( CancellationTokenSource cts = null,
		                               params CryptoExchangeId[] exchangeIds )
		{
			Logger.Info ( "Starting Bot" );

			ConfigManager<CoreConfig>.Load ( );

			Cts       = cts ?? new CancellationTokenSource ( );
			IsRunning = true;
			await FiatConverter.StartMonitor ( );

			Exchanges = ImmutableDictionary<CryptoExchangeId, ICryptoExchange>.Empty
				.AddRange ( AllExchanges.Where ( x => exchangeIds.Contains ( x.Key ) ) );

			InitExchanges ( );

			Cts.Token.Register ( Stop );
		}

		public async Task StartAsync ( CancellationTokenSource cts = null ) =>
			await StartAsync ( cts, AllExchanges.Keys.ToArray ( ) );

		private void InitExchanges ( )
		{
			foreach ( var exchange in Exchanges.Values )
			{
				exchange.Next    += Next;
				exchange.Changed += Changed;

				//CompareTable.AddExchange ( exchange );

				exchange.StartReceivingAsync ( Cts );
			}

			IsInitialized = true;
		}

		public void Stop ( )
		{
			if ( !IsRunning )
				return;

			Logger.Info ( "Stopping Bot" );

			IsRunning = false;
			FiatConverter.StopMonitor ( );
			if ( !Cts.IsCancellationRequested )
				Cts.Cancel ( false );

			Terminate?.Invoke ( this );
		}
	}

	public delegate void TerminateDelegate ( Bot bot );
}