using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Persistence;
using CryptoTickerBot.Helpers;
using NLog;
using Tababular;

// ReSharper disable VirtualMemberNeverOverridden.Global

namespace CryptoTickerBot.Exchanges.Core
{
	public abstract class CryptoExchangeBase : IObservable<CryptoCoin>
	{
		public delegate void OnUpdateDelegate ( CryptoExchangeBase exchange, CryptoCoin coin );

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		protected static readonly List<string> KnownSymbols = new List<string>
		{
			"BTC",
			"LTC",
			"ETH",
			"BCH"
		};

		public string Name { get; }
		public string Url { get; }
		public string TickerUrl { get; }
		public CryptoExchangeId Id { get; }
		public Dictionary<CryptoCoinId, CryptoCoin> ExchangeData { get; protected set; }
		public ImmutableHashSet<IObserver<CryptoCoin>> Observers { get; protected set; }
		public Dictionary<CryptoCoinId, decimal> DepositFees { get; }
		public Dictionary<CryptoCoinId, decimal> WithdrawalFees { get; }
		public decimal BuyFees { get; }
		public decimal SellFees { get; }
		public bool IsStarted { get; protected set; }
		public virtual bool IsComplete => ExchangeData.Count == KnownSymbols.Count;
		public DateTime StartTime { get; private set; }
		public TimeSpan UpTime => DateTime.UtcNow - StartTime;
		public DateTime LastUpdate { get; protected set; }
		public TimeSpan Age => DateTime.UtcNow - LastUpdate;
		public DateTime LastChange { get; protected set; }
		public TimeSpan LastChangeDuration => DateTime.UtcNow - LastChange;
		public int Count => ExchangeData.Count;

		public CryptoCoin this [ CryptoCoinId symbol ]
		{
			get => ExchangeData[symbol];
			set => ExchangeData[symbol] = value;
		}

		protected CryptoExchangeBase ( CryptoExchangeId id )
		{
			ExchangeData = new Dictionary<CryptoCoinId, CryptoCoin> ( );
			Observers    = ImmutableHashSet<IObserver<CryptoCoin>>.Empty;
			Id           = id;

			var exchange = UnitOfWork.Get ( u => u.Exchanges.Get ( id ) );

			if ( exchange == null )
			{
				Logger.Error ( "Exchange info not found in repository." );
				return;
			}

			Name           = exchange.Name;
			Url            = exchange.Url;
			TickerUrl      = exchange.TickerUrl;
			BuyFees        = exchange.BuyFees;
			SellFees       = exchange.SellFees;
			WithdrawalFees = exchange.WithdrawalFees.ToDictionary ( x => x.CoinId, x => x.Value );
			DepositFees    = exchange.DepositFees.ToDictionary ( x => x.CoinId, x => x.Value );
		}

		public IDisposable Subscribe ( IObserver<CryptoCoin> observer )
		{
			Observers = Observers.Add ( observer );

			return Disposable.Create ( ( ) => Observers = Observers.Remove ( observer ) );
		}

		public IDisposable Subscribe ( CryptoExchangeSubscription subscription )
		{
			Observers = Observers.Add ( subscription );
			subscription.Disposable = Disposable.Create (
				( ) => Observers = Observers.Remove ( subscription )
			);

			return subscription.Disposable;
		}

		public void ClearObservers ( ) =>
			Observers = ImmutableHashSet<IObserver<CryptoCoin>>.Empty;

		public abstract Task GetExchangeData ( CancellationToken ct );

		public async Task StartMonitor ( CancellationToken ct = default )
		{
			while ( !ct.IsCancellationRequested )
				try
				{
					StartTime = DateTime.UtcNow;
					IsStarted = true;
					Logger.Debug ( $"Starting {Name,-12} receiver." );
					await GetExchangeData ( ct );
					IsStarted = false;
					Logger.Debug ( $"{Name,-12} receiver terminated." );
				}
				catch ( TaskCanceledException )
				{
					Logger.Info ( $"{Name,-12} task cancelled." );
				}
				catch ( Exception e )
				{
					Logger.Error ( e );
					await Task.Delay ( 60000, ct );
				}
		}

		protected void Update ( dynamic data, string symbol )
		{
			CryptoCoin old = null;
			var id = (CryptoCoinId) Enum.Parse ( typeof ( CryptoCoinId ), symbol, true );
			if ( ExchangeData.ContainsKey ( id ) )
				old = ExchangeData[id].Clone ( );
			ExchangeData[id] = new CryptoCoin ( symbol );

			DeserializeData ( data, id );

			ApplyFees ( id );

			LastUpdate = DateTime.UtcNow;
			Next?.Invoke ( this, ExchangeData[id].Clone ( ) );

			if ( ExchangeData[id] != old ) OnChanged ( this, ExchangeData[id] );
		}

		protected abstract void DeserializeData ( dynamic data, CryptoCoinId id );

		protected void ApplyFees ( CryptoCoinId id )
		{
			var coin = ExchangeData[id];
			coin.LowestAsk   += coin.LowestAsk * BuyFees / 100m;
			coin.HighestBid  += coin.HighestBid * SellFees / 100m;
			ExchangeData[id] =  coin;
		}

		public List<IList<object>> ToSheetRows ( ) =>
			ExchangeData.Values.OrderBy ( coin => coin.Symbol ).Select ( coin => coin.ToSheetsRow ( ) ).ToList ( );

		public event OnUpdateDelegate Changed;
		public event OnUpdateDelegate Next;

		public void OnChanged ( CryptoExchangeBase exchange, CryptoCoin coin )
		{
			Changed?.Invoke ( exchange, coin.Clone ( ) );
			LastChange = DateTime.UtcNow;

			foreach ( var observer in Observers )
				observer.OnNext ( ExchangeData[coin.Id].Clone ( ) );
		}

		public override string ToString ( ) =>
			$"{Name,-12} {UpTime:hh\\:mm\\:ss} {Age:hh\\:mm\\:ss} {LastChangeDuration:hh\\:mm\\:ss}";

		public string ToTable ( FiatCurrency fiat )
		{
			var formatter = new TableFormatter ( );
			var objects = new List<object> ( );

			foreach ( var coin in ExchangeData.Values.OrderBy ( x => x.Symbol ) )
				objects.Add ( new
				{
					coin.Symbol,
					Bid    = $"{FiatConverter.ToString ( coin.HighestBid, FiatCurrency.USD, fiat )}",
					Ask    = $"{FiatConverter.ToString ( coin.LowestAsk, FiatCurrency.USD, fiat )}",
					Spread = $"{coin.SpreadPercentange:P}"
				} );

			return formatter.FormatObjects ( objects );
		}
	}
}