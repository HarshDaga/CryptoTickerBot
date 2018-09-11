using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Telegram.Menus.Abstractions;
using CryptoTickerBot.Telegram.Subscriptions;
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
			Handlers["edit subscription"] = DummyHandler;
			Handlers["back"]              = BackHandler;
		}

		private async Task<TelegramKeyboardMenuBase> AddSubscriptionHandler ( CallbackQuery query )
		{
			var exchangeId = await GetExchangeIdAsync ( );
			if ( exchangeId is null )
				return this;

			var threshold = await GetThresholdAsync ( );
			if ( threshold == -1 )
				return this;

			var isSilent = await GetBoolAsync ( "Keep Silent?" );
			var symbols = await GetSymbolsAsync ( );

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

		private async Task<decimal> GetThresholdAsync ( )
		{
			await RequestReplyAsync ( "Enter the threshold%" );

			var message = await GetMessageAsync ( );

			if ( decimal.TryParse ( message.Text.Trim ( '%' ), out var threshold ) )
				return threshold;

			await SendTextBlockAsync ( $"{message.Text} is not a valid percentage value" );

			return -1;
		}

		private async Task<List<string>> GetSymbolsAsync ( )
		{
			await RequestReplyAsync ( "Enter the symbols" );

			var message = await GetMessageAsync ( );

			return message.Text
				.Split ( " ,".ToCharArray ( ), StringSplitOptions.RemoveEmptyEntries )
				.ToList ( );
		}
	}
}