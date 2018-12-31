using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Exchanges;
using CryptoTickerBot.Core.Helpers;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Data.Configs;
using CryptoTickerBot.Data.Domain;
using EnumsNET;
using JetBrains.Annotations;
using NLog;
using static CryptoTickerBot.Data.Domain.CryptoExchangeId;

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
					[Kraken]    = new KrakenExchange ( )
					//[Zebpay]    = new ZebpayExchange ( )
				} );
		}

		public void Dispose ( )
		{
			Cts?.Dispose ( );
		}

		[UsedImplicitly]
		public event TerminateDelegate Terminate;

		public event StartDelegate Start;

		[UsedImplicitly]
		public event OnUpdateDelegate Changed;

		[UsedImplicitly]
		public event OnUpdateDelegate Next;

		public async Task StartAsync ( CancellationTokenSource cts = null,
		                               params CryptoExchangeId[] exchangeIds )
		{
			Logger.Info ( "Starting Bot" );

			StartTime = DateTime.UtcNow;

			Cts       = cts ?? new CancellationTokenSource ( );
			IsRunning = true;
			await FiatConverter.StartMonitorAsync ( ).ConfigureAwait ( false );

			Exchanges = ImmutableDictionary<CryptoExchangeId, ICryptoExchange>.Empty
				.AddRange ( AllExchanges.Where ( x => exchangeIds.Contains ( x.Key ) ) );

			InitExchanges ( );

			Cts.Token.Register ( async ( ) => await StopAsync ( ).ConfigureAwait ( false ) );

			Start?.Invoke ( this );
		}

		public async Task StartAsync ( CancellationTokenSource cts = null ) =>
			await StartAsync ( cts, AllExchanges.Keys.ToArray ( ) ).ConfigureAwait ( false );

		public async Task StopAsync ( )
		{
			if ( !IsRunning )
				return;
			IsRunning = false;

			Logger.Info ( "Stopping Bot" );

			FiatConverter.StopMonitor ( );
			if ( !Cts.IsCancellationRequested )
				Cts.Cancel ( );

			var services = Services.ToList ( );

			foreach ( var service in services )
				await DetachAsync ( service ).ConfigureAwait ( false );

			Terminate?.Invoke ( this );
		}

		public void RestartExchangeMonitors ( )
		{
			foreach ( var exchange in Exchanges.Values )
			{
				exchange.StopReceivingAsync ( );
				exchange.StartReceivingAsync ( Cts.Token );
			}
		}

		public bool ContainsService ( IBotService service ) =>
			Services.Contains ( service );

		public async Task AttachAsync ( IBotService service )
		{
			if ( ContainsService ( service ) )
				return;

			Services = Services.Add ( service );
			await service.AttachToAsync ( this ).ConfigureAwait ( false );
		}

		public async Task DetachAsync ( IBotService service )
		{
			if ( !ContainsService ( service ) )
				return;

			Services = Services.Remove ( service );
			await service.DetachAsync ( ).ConfigureAwait ( false );
		}

		public async Task DetachAllAsync<T> ( ) where T : IBotService
		{
			var services = Services.OfType<T> ( ).ToList ( );

			foreach ( var service in services )
				await DetachAsync ( service ).ConfigureAwait ( false );
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
			ConfigManager<CoreConfig>.Load ( );

			foreach ( var exchange in Exchanges.Values )
			{
				exchange.Next    += OnNextAsync;
				exchange.Changed += OnChangedAsync;

				//CompareTable.AddExchange ( exchange );

				exchange.StartReceivingAsync ( Cts?.Token );
			}

			IsInitialized = true;
		}

		private async Task OnNextAsync ( ICryptoExchange exchange,
		                                 CryptoCoin coin )
		{
			foreach ( var service in Services )
				await service.OnNextAsync ( exchange, coin ).ConfigureAwait ( false );
			Next?.Invoke ( exchange, coin );
		}

		private async Task OnChangedAsync ( ICryptoExchange exchange,
		                                    CryptoCoin coin )
		{
			foreach ( var service in Services )
				await service.OnChangedAsync ( exchange, coin ).ConfigureAwait ( false );
			Changed?.Invoke ( exchange, coin );
		}
	}
}