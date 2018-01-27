using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Extensions;
using NLog;

namespace CryptoTickerBot.Exchanges
{
	public class CryptoExchangeObserver : IObserver<CryptoCoin>
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		private readonly Dictionary<string, List<CryptoCoin>> history;
		private readonly Dictionary<string, List<PriceChange>> priceChanges;

		private Dictionary<long, List<SubscriptionInfo>> significantChangeSubscriptions;

		public CryptoExchangeBase Exchange { get; }

		public string SubscriptionsFileName { get; } = "Subscriptions.json";

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

		public ResumableSubscription Subscribe ( long id, decimal threshold,
			Action<CryptoExchangeBase, CryptoCoin, CryptoCoin> action )
		{
			if ( !significantChangeSubscriptions.ContainsKey ( id ) )
				significantChangeSubscriptions[id] = new List<SubscriptionInfo> ( );

			var info = new SubscriptionInfo ( Exchange, threshold );
			info.Init ( );
			info.Changed += action;
			significantChangeSubscriptions[id].Add ( info );

			return new ResumableSubscription
			{
				Exchange = Exchange.Id,
				Id = id,
				LastSignificantPrice = Exchange.ExchangeData,
				Threshhold = threshold
			};
		}

		public void Subscribe ( ResumableSubscription subscription,
			Action<CryptoExchangeBase, CryptoCoin, CryptoCoin> action )
		{
			if ( subscription.Exchange != Exchange.Id )
				return;

			if ( !significantChangeSubscriptions.ContainsKey ( subscription.Id ) )
				significantChangeSubscriptions[subscription.Id] = new List<SubscriptionInfo> ( );

			var info = new SubscriptionInfo ( Exchange, subscription );
			info.Changed += action;
			significantChangeSubscriptions[subscription.Id].Add ( info );
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

		public class ResumableSubscription
		{
			public CryptoExchange Exchange { get; set; }
			public long Id { get; set; }
			public decimal Threshhold { get; set; }
			public Dictionary<string, CryptoCoin> LastSignificantPrice { get; set; }
		}

		private class SubscriptionInfo
		{
			public CryptoExchangeBase Exchange { get; }
			public Dictionary<string, CryptoCoin> LastSignificantPrice { get; }
			public decimal Threshold { get; }

			public SubscriptionInfo ( CryptoExchangeBase exchange, decimal threshold )
			{
				Exchange = exchange;
				Threshold = threshold;
				LastSignificantPrice = new Dictionary<string, CryptoCoin> ( );
			}

			public SubscriptionInfo ( CryptoExchangeBase exchange, ResumableSubscription subscription )
			{
				Exchange = exchange;
				Threshold = subscription.Threshhold;
				LastSignificantPrice = subscription.LastSignificantPrice;
			}

			public event Action<CryptoExchangeBase, CryptoCoin, CryptoCoin> Changed;

			public void Init ( )
			{
				foreach ( var coin in Exchange.ExchangeData )
					LastSignificantPrice[coin.Key] = coin.Value;
			}

			public void Process ( CryptoCoin coin )
			{
				if ( !LastSignificantPrice.ContainsKey ( coin.Symbol ) )
					LastSignificantPrice[coin.Symbol] = coin.Clone ( );

				var change = coin - LastSignificantPrice[coin.Symbol];
				var percentage = Math.Abs ( change.Percentage );
				if ( percentage >= Threshold )
				{
					Changed?.BeginInvoke ( Exchange, LastSignificantPrice[coin.Symbol], coin, null, null );
					LastSignificantPrice[coin.Symbol] = coin.Clone ( );
				}
			}
		}
	}
}