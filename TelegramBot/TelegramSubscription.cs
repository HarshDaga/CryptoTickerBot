using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Exchanges.Core;
using JetBrains.Annotations;
using NLog;
using CryptoCoin = CryptoTickerBot.CryptoCoin;

namespace TelegramBot
{
	public class TelegramSubscription : CryptoExchangeSubscription
	{
		public delegate void TelegramSubscriptionValueChangeNotificationDelegate (
			TelegramSubscription subscription,
			CryptoCoin prevPrice,
			CryptoCoin newPrice
		);

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public int Id { get; }
		public long ChatId { get; }
		public string UserName { get; }
		public CryptoExchangeId ExchangeId { get; }
		public decimal Threshold { get; }
		public IDictionary<CryptoCoinId, CryptoCoin> LastSignificantPrice { get; }
		public ISet<CryptoCoinId> Coins { get; }

		public CryptoCoin this [ CryptoCoinId coinId ] =>
			LastSignificantPrice[coinId];

		public TelegramSubscription (
			CryptoExchangeBase exchange,
			int id,
			long chatId,
			string userName,
			decimal threshold,
			IEnumerable<CryptoCoinId> coins,
			IDictionary<CryptoCoinId, CryptoCoin> lastSignificantPrice = null
		) : base ( exchange )
		{
			Id         = id;
			ChatId     = chatId;
			UserName   = userName;
			ExchangeId = exchange.Id;
			Threshold  = threshold;
			LastSignificantPrice = new ConcurrentDictionary<CryptoCoinId, CryptoCoin> (
				lastSignificantPrice ?? exchange.ExchangeData
			);
			Coins = coins.ToImmutableHashSet ( );
		}

		public TelegramSubscription (
			CryptoExchangeBase exchange,
			TeleSubscription subscription )
			: base ( exchange )
		{
			Id         = subscription.Id;
			ChatId     = subscription.ChatId;
			UserName   = subscription.UserName;
			ExchangeId = exchange.Id;
			Threshold  = subscription.Threshold;
			LastSignificantPrice = subscription.LastSignificantPrice.ToDictionary (
				x => x.Key,
				x => new CryptoCoin ( x.Value )
			);
			Coins = subscription.Coins.Select ( c => c.Id ).ToImmutableHashSet ( );
		}

		[UsedImplicitly]
		public event TelegramSubscriptionValueChangeNotificationDelegate Changed;

		[UsedImplicitly]
		public event TelegramSubscriptionValueChangeNotificationDelegate Updated;

		public override void OnNext ( CryptoCoin coin )
		{
			if ( !Coins.Contains ( coin.Id ) )
				return;

			if ( !LastSignificantPrice.ContainsKey ( coin.Id ) )
			{
				LastSignificantPrice[coin.Id] = coin.Clone ( );
				Changed?.Invoke ( this, null, coin.Clone ( ) );
			}

			var change = coin - LastSignificantPrice[coin.Id];
			var percentage = Math.Abs ( change.Percentage );
			Updated?.Invoke ( this, LastSignificantPrice[coin.Id].Clone ( ), coin.Clone ( ) );

			if ( percentage >= Threshold )
			{
				Changed?.Invoke ( this, LastSignificantPrice[coin.Id].Clone ( ), coin.Clone ( ) );
				LastSignificantPrice[coin.Id] = coin.Clone ( );
				Logger.Info (
					$"Invoked subscription for {UserName} @ {coin.Average:C} {coin.Symbol} {Exchange.Name}"
				);
			}
		}

		public void Update ( TeleSubscription subscription )
		{
			foreach ( var kp in LastSignificantPrice )
			{
				subscription.LastSignificantPrice[kp.Key].HighestBid = kp.Value.HighestBid;
				subscription.LastSignificantPrice[kp.Key].LowestAsk  = kp.Value.LowestAsk;
				subscription.LastSignificantPrice[kp.Key].Time       = kp.Value.Time;
			}
		}
	}
}