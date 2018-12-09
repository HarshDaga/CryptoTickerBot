using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Collections;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Data.Configs;
using CryptoTickerBot.Data.Domain;
using Fody;
using NLog;
using Polly;
using Tababular;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable StaticMemberInGenericType

namespace CryptoTickerBot.Core.Abstractions
{
	[ConfigureAwait ( false )]
	public abstract class CryptoExchangeBase<T> : ICryptoExchange
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		protected static CoreConfig CoreConfig => ConfigManager<CoreConfig>.Instance;

		public string Name { get; }
		public string Url { get; }
		public string TickerUrl { get; protected set; }
		public OrderedDictionary<string, string> SymbolMappings { get; protected set; }
		public CryptoExchangeId Id { get; }
		public ImmutableHashSet<string> BaseSymbols => Markets.BaseSymbols;
		public Markets Markets { get; protected set; }
		public IDictionary<string, CryptoCoin> ExchangeData { get; protected set; }
		public ImmutableHashSet<IObserver<CryptoCoin>> Observers { get; protected set; }
		public IDictionary<string, decimal> DepositFees { get; }
		public IDictionary<string, decimal> WithdrawalFees { get; }
		public decimal BuyFees { get; }
		public decimal SellFees { get; }
		public TimeSpan PollingRate { get; }
		public TimeSpan CooldownPeriod { get; }
		public bool IsStarted { get; protected set; }

		public DateTime StartTime { get; private set; }
		public TimeSpan UpTime => DateTime.UtcNow - StartTime;
		public DateTime LastUpdate { get; protected set; }
		public TimeSpan LastUpdateDuration => DateTime.UtcNow - LastUpdate;
		public DateTime LastChange { get; protected set; }
		public TimeSpan LastChangeDuration => DateTime.UtcNow - LastChange;
		public int Count => ExchangeData.Count;
		public Policy Policy { get; set; }

		public CryptoCoin this [ string symbol ]
		{
			get => ExchangeData.ContainsKey ( symbol ) ? ExchangeData[symbol].Clone ( ) : null;
			set => ExchangeData[symbol] = value.Clone ( );
		}

		public CryptoCoin this [ string baseSymbol,
		                         string symbol ] =>
			Markets[baseSymbol, symbol];

		private CancellationTokenSource cts;

		protected CryptoExchangeBase ( CryptoExchangeId id )
		{
			ExchangeData = new ConcurrentDictionary<string, CryptoCoin> ( );
			Observers    = ImmutableHashSet<IObserver<CryptoCoin>>.Empty;
			Id           = id;

			var exchange = CoreConfig.ExchangeApiInfo?.FirstOrDefault ( x => x.Id == id );

			if ( exchange == null )
			{
				Logger.Error ( $"Exchange info for {id} not found." );
				return;
			}

			Name           = exchange.Name;
			Url            = exchange.Url;
			TickerUrl      = exchange.TickerUrl;
			BuyFees        = exchange.BuyFees;
			SellFees       = exchange.SellFees;
			PollingRate    = exchange.PollingRate;
			CooldownPeriod = exchange.CooldownPeriod;
			WithdrawalFees = new Dictionary<string, decimal> ( exchange.WithdrawalFees );
			DepositFees    = new Dictionary<string, decimal> ( exchange.DepositFees );
			Markets        = new Markets ( exchange.BaseSymbols );
			SymbolMappings = exchange.SymbolMappings.Clone ( );

			Policy = Policy
				.Handle<TaskCanceledException> ( )
				.Or<Exception> ( )
				.WaitAndRetryForeverAsync (
					i => CooldownPeriod,
					( exception,
					  retryCount,
					  span ) =>
					{
						Logger.Error ( exception );
						return Task.CompletedTask;
					}
				);
		}

		public event OnUpdateDelegate Changed;
		public event OnUpdateDelegate Next;

		public virtual IDisposable Subscribe ( IObserver<CryptoCoin> observer )
		{
			Observers = Observers.Add ( observer );

			return Disposable.Create ( ( ) => Observers = Observers.Remove ( observer ) );
		}

		public virtual async Task StartReceivingAsync ( CancellationTokenSource tokenSource = null )
		{
			cts = tokenSource ?? new CancellationTokenSource ( );

			await Policy.ExecuteAsync ( async ct =>
			{
				StartTime = DateTime.UtcNow;
				IsStarted = true;
				Logger.Debug ( $"Starting {Name,-12} receiver." );

				ExchangeData = new ConcurrentDictionary<string, CryptoCoin> ( );
				await GetExchangeDataAsync ( ct );

				IsStarted = false;
				Logger.Debug ( $"{Name,-12} receiver terminated." );
			}, cts.Token );
		}

		public virtual async Task StopReceivingAsync ( )
		{
			cts.Cancel ( );
			await Task.CompletedTask;
		}

		public void Unsubscribe ( IObserver<CryptoCoin> subscription )
		{
			Observers = Observers.Remove ( subscription );
		}

		public virtual CryptoCoin GetWithFees ( string symbol )
		{
			if ( !ExchangeData.TryGetValue ( symbol, out var coin ) )
				return null;

			coin            =  coin.Clone ( );
			coin.LowestAsk  += coin.LowestAsk * BuyFees / 100m;
			coin.HighestBid += coin.HighestBid * SellFees / 100m;

			return coin;
		}

		public virtual string ToTable ( params string[] symbols )
		{
			var formatter = new TableFormatter ( );
			var objects = new List<object> ( );

			foreach (
				var coin
				in
				ExchangeData.Values
					.Where ( x => symbols.Any ( s => x.Symbol.Contains ( s.ToUpper ( ) ) ) )
					.OrderBy ( x => x.Symbol )
			)
				objects.Add ( new
				{
					coin.Symbol,
					Bid    = coin.HighestBid,
					Ask    = coin.LowestAsk,
					Spread = $"{coin.SpreadPercentage:P}"
				} );

			return formatter.FormatObjects ( objects );
		}

		protected abstract Task GetExchangeDataAsync ( CancellationToken ct );

		protected virtual string CleanAndExtractSymbol ( string symbol )
		{
			symbol = Regex.Replace ( symbol, @"[\\\/-]", "" );
			symbol = SymbolMappings.Aggregate ( symbol, ( current,
			                                              mapping ) =>
				                                    current.Replace ( mapping.Key, mapping.Value ) );

			return symbol;
		}

		protected virtual void Update ( T data,
		                                string symbol )
		{
			symbol = CleanAndExtractSymbol ( symbol );

			if ( ExchangeData.TryGetValue ( symbol, out var old ) )
				old = old.Clone ( );
			ExchangeData[symbol] = new CryptoCoin ( symbol );

			DeserializeData ( data, symbol );
			Markets.AddOrUpdate ( ExchangeData[symbol] );

			LastUpdate = DateTime.UtcNow;
			OnNext ( ExchangeData[symbol] );

			if ( !ExchangeData[symbol].HasSameValues ( old ) )
				OnChanged ( ExchangeData[symbol] );
		}

		protected abstract void DeserializeData ( T data,
		                                          string id );

		protected void OnChanged ( CryptoCoin coin )
		{
			Changed?.Invoke ( this, coin.Clone ( ) );
			LastChange = DateTime.UtcNow;

			foreach ( var observer in Observers )
				observer.OnNext ( ExchangeData[coin.Symbol].Clone ( ) );
		}

		protected void OnNext ( CryptoCoin coin )
		{
			Next?.Invoke ( this, coin.Clone ( ) );
		}

		public override string ToString ( ) =>
			$"{Name,-12} {UpTime:hh\\:mm\\:ss} {LastUpdateDuration:hh\\:mm\\:ss} {LastChangeDuration:hh\\:mm\\:ss}";
	}
}