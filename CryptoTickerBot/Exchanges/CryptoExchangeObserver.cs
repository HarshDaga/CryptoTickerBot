using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Extensions;

namespace CryptoTickerBot.Exchanges
{
	public class CryptoExchangeObserver : IObserver<CryptoCoin>
	{
		private readonly Dictionary<string, List<CryptoCoin>> history;
		private readonly Dictionary<string, List<PriceChange>> priceChanges;

		private readonly Dictionary<long, List<SubscriptionInfo>> significantChangeSubscriptions;

		public CryptoExchangeBase Exchange { get; }

		public CryptoExchangeObserver ( CryptoExchangeBase exchange )
		{
			Exchange = exchange;
			history = new Dictionary<string, List<CryptoCoin>> ( );
			priceChanges = new Dictionary<string, List<PriceChange>> ( );
			significantChangeSubscriptions = new Dictionary<long, List<SubscriptionInfo>> ( );
		}

		public void OnNext ( CryptoCoin coin )
		{
			if ( !history.ContainsKey ( coin.Symbol ) )
			{
				history[coin.Symbol] = new List<CryptoCoin> ( );
				priceChanges[coin.Symbol] = new List<PriceChange> ( );
			}

			if ( history[coin.Symbol].Any ( ) )
				priceChanges[coin.Symbol].Add ( coin - history[coin.Symbol].Last ( ) );

			foreach ( var subscriptionInfos in significantChangeSubscriptions.Values )
			foreach ( var subscriptionInfo in subscriptionInfos )
				subscriptionInfo.Process ( coin );

			history[coin.Symbol].Add ( coin );

			Next?.Invoke ( Exchange, coin );
		}

		public void OnError ( Exception error )
		{
		}

		public void OnCompleted ( )
		{
		}

		public void Subscribe ( long id, decimal threshold,
			Action<CryptoExchangeBase, CryptoCoin, CryptoCoin> action )
		{
			if ( !significantChangeSubscriptions.ContainsKey ( id ) )
				significantChangeSubscriptions[id] = new List<SubscriptionInfo> ( );

			var info = new SubscriptionInfo ( Exchange, threshold );
			info.Init ( );
			info.Changed += action;
			significantChangeSubscriptions[id].Add ( info );
		}

		public void Unsubscribe ( long id )
		{
			if ( significantChangeSubscriptions.ContainsKey ( id ) )
				significantChangeSubscriptions[id].Clear ( );
		}

		public event Action<CryptoExchangeBase, CryptoCoin> Next;

		public IList<CryptoCoin> History ( string symbol, TimeSpan timeSpan )
		{
			var start = DateTime.Now.Subtract ( timeSpan );
			return history[symbol].SkipWhile ( x => x.Time < start ).ToList ( );
		}

		public IList<CryptoCoin> History ( string symbol, int count ) =>
			history[symbol].TakeLast ( count );

		private class SubscriptionInfo
		{
			private readonly CryptoExchangeBase exchange;
			private readonly Dictionary<string, CryptoCoin> lastSignificantPrice;
			private readonly decimal threshold;

			public SubscriptionInfo ( CryptoExchangeBase exchange, decimal threshold )
			{
				this.exchange = exchange;
				this.threshold = threshold;
				lastSignificantPrice = new Dictionary<string, CryptoCoin> ( );
			}

			public event Action<CryptoExchangeBase, CryptoCoin, CryptoCoin> Changed;

			public void Init ( )
			{
				foreach ( var coin in exchange.ExchangeData )
					lastSignificantPrice[coin.Key] = coin.Value;
			}

			public void Process ( CryptoCoin coin )
			{
				if ( !lastSignificantPrice.ContainsKey ( coin.Symbol ) )
					lastSignificantPrice[coin.Symbol] = coin.Clone ( );

				var change = coin - lastSignificantPrice[coin.Symbol];
				var percentage = Math.Abs ( change.Percentage );
				if ( percentage >= threshold )
				{
					Changed?.BeginInvoke ( exchange, lastSignificantPrice[coin.Symbol], coin, null, null );
					lastSignificantPrice[coin.Symbol] = coin.Clone ( );
				}
			}
		}
	}
}