using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.WebSocket.Extensions;
using JetBrains.Annotations;
using TelegramBot.Core;

namespace CryptoTickerBot.WebSocket.Messages
{
	public class TeleBotSubscriptionSummary
	{
		public int Id { get; }
		public long ChatId { get; }
		public string UserName { get; }
		public string Exchange { get; }
		public decimal Threshold { get; }
		public Dictionary<string, decimal> LastSignificantPrice { get; }

		[UsedImplicitly]
		public Dictionary<string, decimal?> CurrentPrice { get; }

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
				.ToDictionary ( kp => kp.Key.ToString ( ), kp => kp.Value.Average.RoundOff ( ) );

			CurrentPrice = new Dictionary<string, decimal?> ( );
			foreach ( var coinId in subscription.LastSignificantPrice.Keys )
			{
				var price = bot.Ctb.Exchanges[subscription.ExchangeId][coinId]?.Average.RoundOff ( );
				CurrentPrice[coinId.ToString ( )] = price;
			}
		}
	}
}