using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NLog;
using TelegramBot;
using TelegramBot.Core;

namespace CryptoTickerBot.WebSocket.Messages
{
	public class TeleBotSubscriptionSummary
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public int Id { get; }
		public long ChatId { get; }
		public string UserName { get; }
		public string Exchange { get; }
		public decimal Threshold { get; }
		public Dictionary<string, CoinValueSummary> LastSignificantPrice { get; }

		[UsedImplicitly]
		public Dictionary<string, CoinValueSummary> CurrentPrice { get; }

		[UsedImplicitly]
		public Dictionary<string, PriceChange?> PriceChanges { get; }

		public TeleBotSubscriptionSummary ( TeleBot bot, int subscriptionId )
		{
			var subscription = bot.Subscriptions.FirstOrDefault ( x => x.Id == subscriptionId );
			if ( subscription == null )
				return;

			Id        = subscription.Id;
			ChatId    = subscription.ChatId;
			UserName  = subscription.UserName;
			Exchange  = subscription.ExchangeId.ToString ( );
			Threshold = subscription.Threshold;
			LastSignificantPrice = subscription.LastSignificantPrice
				.ToDictionary ( kp => kp.Key.ToString ( ), kp => new CoinValueSummary ( kp.Value ) );

			CurrentPrice = new Dictionary<string, CoinValueSummary> ( );
			PriceChanges = new Dictionary<string, PriceChange?> ( );

			UpdateLatest ( bot, subscription );
		}

		private void UpdateLatest ( TeleBot bot, TelegramSubscription subscription )
		{
			try
			{
				foreach ( var coinId in subscription.LastSignificantPrice.Keys )
				{
					var latest = bot.Ctb.Exchanges[subscription.ExchangeId][coinId]?.Clone ( );

					while ( latest?.Average == 0 )
						latest = bot.Ctb.Exchanges[subscription.ExchangeId][coinId]?.Clone ( );

					if ( latest != null )
						PriceChanges[coinId.ToString ( )] = latest - subscription.LastSignificantPrice[coinId];
					else
						PriceChanges[coinId.ToString ( )] = null;

					CurrentPrice[coinId.ToString ( )] = latest;
				}
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		public class CoinValueSummary
		{
			[JsonIgnore]
			public string Symbol { get; }

			public decimal HighestBid { get; }
			public decimal LowestAsk { get; }
			public decimal Average => ( LowestAsk + HighestBid ) / 2;
			public decimal Spread => LowestAsk - HighestBid;
			public decimal SpreadPercentange => Spread / ( LowestAsk + HighestBid ) * 2;
			public DateTime Time { get; }

			public CoinValueSummary ( CryptoCoin coin )
			{
				Symbol     = coin.Symbol;
				HighestBid = coin.HighestBid;
				LowestAsk  = coin.LowestAsk;
				Time       = coin.Time;
			}

			public static implicit operator CoinValueSummary ( [CanBeNull] CryptoCoin coin ) =>
				coin is null ? null : new CoinValueSummary ( coin );
		}
	}
}