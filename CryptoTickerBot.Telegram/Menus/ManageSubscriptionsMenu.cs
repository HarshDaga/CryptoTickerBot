using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Telegram.Menus.Abstractions;
using CryptoTickerBot.Telegram.Subscriptions;
using Humanizer;
using MoreLinq;
using Telegram.Bot.Types;

namespace CryptoTickerBot.Telegram.Menus
{
	internal class ManageSubscriptionsMenu : TelegramKeyboardMenuBase
	{
		public ManageSubscriptionsMenu ( TelegramBot telegramBot,
		                                 User user,
		                                 Chat chat,
		                                 TelegramKeyboardMenuBase parent = null ) :
			base ( telegramBot, user, chat, "Manage Subscriptions", parent: parent )
		{
			Labels = new[] {"add subscription", "edit subscription"}
				.Batch ( 1 )
				.ToList ( );
			AddWideLabel ( "back" );

			BuildKeyboard ( );
			AddHandlers ( );
		}

		private void AddHandlers ( )
		{
			Handlers["add subscription"]  = AddSubscriptionHandler;
			Handlers["edit subscription"] = EditSubscriptionHandler;
			Handlers["back"]              = BackHandler;
		}

		private async Task<TelegramKeyboardMenuBase> AddSubscriptionHandler ( CallbackQuery query )
		{
			var exchangeId = await ReadExchangeIdAsync ( );
			if ( exchangeId is null )
				return this;

			var threshold = await ReadThresholdAsync ( );
			if ( threshold == -1 )
				return this;

			var isSilent = await ReadBoolAsync ( "Keep Silent?" ) ?? false;
			var symbols = await ReadSymbolsAsync ( );

			var subscription = new TelegramPercentChangeSubscription (
				Chat,
				User,
				exchangeId.Value,
				threshold / 100m,
				isSilent,
				symbols
			);

			await TelegramBot.AddOrUpdateSubscription ( subscription );

			return this;
		}

		private async Task<TelegramKeyboardMenuBase> EditSubscriptionHandler ( CallbackQuery query )
		{
			var subscriptions = TelegramBot.Data.PercentChangeSubscriptions
				.Where ( x => x.ChatId.Identifier == Chat.Id )
				.ToList ( );

			var exchangeId =
				await ReadExchangeIdAsync ( subscriptions
					                            .Select ( x => x.ExchangeId )
					                            .Distinct ( )
				);
			if ( exchangeId is null )
				return this;

			subscriptions = subscriptions.Where ( x => x.ExchangeId == exchangeId ).ToList ( );

			if ( subscriptions.Count == 1 )
				return new EditSubscriptionMenu ( TelegramBot, subscriptions[0], this );

			var threshold = await ReadThresholdAsync ( subscriptions.Select ( x => x.Threshold ) );
			if ( threshold == -1 )
				return this;

			var subscription = subscriptions.SingleOrDefault ( x => x.Threshold == threshold );
			if ( subscription is null )
				return this;

			return new EditSubscriptionMenu ( TelegramBot, subscription, this );
		}

		private async Task<decimal> ReadThresholdAsync ( )
		{
			await RequestReplyAsync ( "Enter the threshold%" );

			return await ReadPercentage ( );
		}

		private async Task<decimal> ReadThresholdAsync ( IEnumerable<decimal> thresholds )
		{
			var list = thresholds.ToList ( );
			await SendOptionsAsync (
				$"{"subscription".ToQuantity ( list.Count )} found\nChoose threshold%", list, 2 );

			return await ReadPercentage ( );
		}
	}
}