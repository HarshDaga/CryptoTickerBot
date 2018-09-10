using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Exchanges;
using CryptoTickerBot.Core.Helpers;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Domain;
using CryptoTickerBot.Domain.Configs;
using EnumsNET;
using JetBrains.Annotations;
using NLog;
using static CryptoTickerBot.Domain.CryptoExchangeId;

namespace CryptoTickerBot.Core
{
	public class Bot : IBot
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public static readonly ImmutableDictionary<CryptoExchangeId, ICryptoExchange> AllExchanges;
		public ImmutableDictionary<CryptoExchangeId, ICryptoExchange> Exchanges { get; private set; }

		public CancellationTokenSource Cts { get; private set; }

		public bool IsRunning { get; private set; }
		public bool IsInitialized { get; private set; }

		public ImmutableHashSet<IBotService> Services { get; private set; } = ImmutableHashSet<IBotService>.Empty;
		public DateTime StartTime { get; private set; }

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

			StartTime = DateTime.UtcNow;

			ConfigManager<CoreConfig>.Load ( );

			Cts       = cts ?? new CancellationTokenSource ( );
			IsRunning = true;
			await FiatConverter.StartMonitor ( );

			Exchanges = ImmutableDictionary<CryptoExchangeId, ICryptoExchange>.Empty
				.AddRange ( AllExchanges.Where ( x => exchangeIds.Contains ( x.Key ) ) );

			InitExchanges ( );

			Cts.Token.Register ( async ( ) => await StopAsync ( ) );
		}

		public async Task StartAsync ( CancellationTokenSource cts = null ) =>
			await StartAsync ( cts, AllExchanges.Keys.ToArray ( ) );

		public async Task StopAsync ( )
		{
			if ( !IsRunning )
				return;

			Logger.Info ( "Stopping Bot" );

			IsRunning = false;
			FiatConverter.StopMonitor ( );
			if ( !Cts.IsCancellationRequested )
				Cts.Cancel ( false );

			var services = Services.ToList ( );

			foreach ( var service in services )
				await Detach ( service );

			Terminate?.Invoke ( this );
		}

		public bool ContainsService ( IBotService service ) =>
			Services.Contains ( service );

		public async Task Attach ( IBotService service )
		{
			if ( ContainsService ( service ) )
				return;

			Services = Services.Add ( service );
			await service.AttachTo ( this );
		}

		public async Task Detach ( IBotService service )
		{
			if ( !ContainsService ( service ) )
				return;

			Services = Services.Remove ( service );
			await service.Detach ( );
		}

		public async Task DetachAll<T> ( ) where T : IBotService
		{
			var services = Services.OfType<T> ( ).ToList ( );

			foreach ( var service in services )
				await Detach ( service );
		}

		public bool TryGetExchange ( CryptoExchangeId exchangeId,
		                             out ICryptoExchange exchange ) =>
			Exchanges.TryGetValue ( exchangeId, out exchange );

		public bool TryGetExchange ( string exchangeId,
		                             out ICryptoExchange exchange )
		{
			if ( Enums.TryParse ( exchangeId, out CryptoExchangeId id ) )
				return TryGetExchange ( id, out exchange );

			exchange = null;

			return false;
		}

		private void InitExchanges ( )
		{
			foreach ( var exchange in Exchanges.Values )
			{
				exchange.Next    += OnNext;
				exchange.Changed += OnChanged;

				//CompareTable.AddExchange ( exchange );

				exchange.StartReceivingAsync ( Cts );
			}

			IsInitialized = true;
		}

		private async Task OnNext ( ICryptoExchange exchange,
		                            CryptoCoin coin )
		{
			foreach ( var service in Services )
				await service.OnNext ( exchange, coin );
			Next?.Invoke ( exchange, coin );
		}

		private async Task OnChanged ( ICryptoExchange exchange,
		                               CryptoCoin coin )
		{
			foreach ( var service in Services )
				await service.OnChanged ( exchange, coin );
			Changed?.Invoke ( exchange, coin );
		}
	}

	public delegate void TerminateDelegate ( Bot bot );
}