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
		                                 ITelegramKeyboardMenu parent = null ) :
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
			Handlers["add subscription"]  = AddSubscriptionHandlerAsync;
			Handlers["edit subscription"] = EditSubscriptionHandlerAsync;
			Handlers["back"]              = BackHandler;
		}

		private async Task<ITelegramKeyboardMenu> AddSubscriptionHandlerAsync ( CallbackQuery query )
		{
			var exchangeId = await ReadExchangeIdAsync ( ).ConfigureAwait ( false );
			if ( exchangeId is null )
				return this;

			var threshold = await ReadThresholdAsync ( ).ConfigureAwait ( false );
			if ( threshold == -1 )
				return this;

			var isSilent = await ReadBoolAsync ( "Keep Silent?" ).ConfigureAwait ( false ) ?? false;
			var symbols = await ReadSymbolsAsync ( ).ConfigureAwait ( false );

			var subscription = new TelegramPercentChangeSubscription (
				Chat,
				User,
				exchangeId.Value,
				threshold,
				isSilent,
				symbols
			);

			await TelegramBot.AddOrUpdateSubscriptionAsync ( subscription ).ConfigureAwait ( false );

			return this;
		}

		private async Task<ITelegramKeyboardMenu> EditSubscriptionHandlerAsync ( CallbackQuery query )
		{
			var subscriptions = TelegramBot.Data.PercentChangeSubscriptions
				.Where ( x => x.ChatId.Identifier == Chat.Id && x.User == User )
				.ToList ( );

			if ( !subscriptions.Any ( ) )
			{
				await SendTextBlockAsync ( "There are no subscriptions to edit" ).ConfigureAwait ( false );
				return this;
			}

			var exchangeId =
				await ReadExchangeIdAsync ( subscriptions
					                            .Select ( x => x.ExchangeId )
					                            .Distinct ( )
				).ConfigureAwait ( false );
			if ( exchangeId is null )
				return this;

			subscriptions = subscriptions.Where ( x => x.ExchangeId == exchangeId ).ToList ( );

			if ( subscriptions.Count == 1 )
				return new EditSubscriptionMenu ( TelegramBot, subscriptions[0], this );

			var threshold = await ReadThresholdAsync ( subscriptions.Select ( x => x.Threshold ) )
				.ConfigureAwait ( false );
			if ( threshold == -1 )
				return this;

			var subscription = subscriptions.SingleOrDefault ( x => x.Threshold == threshold );
			if ( subscription is null )
				return this;

			return new EditSubscriptionMenu ( TelegramBot, subscription, this );
		}

		private async Task<decimal> ReadThresholdAsync ( )
		{
			await RequestReplyAsync ( "Enter the threshold%" ).ConfigureAwait ( false );

			return await ReadPercentageAsync ( ).ConfigureAwait ( false );
		}

		private async Task<decimal> ReadThresholdAsync ( IEnumerable<decimal> thresholds )
		{
			var list = thresholds.ToList ( );
			await SendOptionsAsync (
					$"{"subscription".ToQuantity ( list.Count )} found\nChoose threshold%",
					list.Select ( x => $"{x:P}" ) )
				.ConfigureAwait ( false );

			return await ReadPercentageAsync ( ).ConfigureAwait ( false );
		}
	}
}