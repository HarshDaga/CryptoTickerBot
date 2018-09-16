using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Telegram.Menus.Abstractions;
using CryptoTickerBot.Telegram.Subscriptions;
using MoreLinq.Extensions;
using Telegram.Bot.Types;

namespace CryptoTickerBot.Telegram.Menus
{
	internal class EditSubscriptionMenu : TelegramKeyboardMenuBase
	{
		public TelegramPercentChangeSubscription Subscription { get; }

		public EditSubscriptionMenu ( TelegramBot telegramBot,
		                              TelegramPercentChangeSubscription subscription,
		                              TelegramKeyboardMenuBase parent = null ) :
			base ( telegramBot, subscription.User, subscription.Chat, "Edit Subscription", parent: parent )
		{
			Subscription = subscription;
			Labels = new[] {"change silence mode", "change threshold", "add symbols", "remove symbols"}
				.Batch ( 2 )
				.ToList ( );
			AddWideLabel ( "delete" );
			AddWideLabel ( "back" );

			BuildKeyboard ( );
			AddHandlers ( );
		}

		private void AddHandlers ( )
		{
			Handlers["change silence mode"] = ChangeSilenceModeHandler;
			Handlers["change threshold"]    = ChangeThresholdHandler;
			Handlers["add symbols"]         = AddSymbolsHandler;
			Handlers["remove symbols"]      = RemoveSymbolsHandler;
			Handlers["delete"]              = DeleteHandler;
			Handlers["back"]                = BackHandler;
		}

		private async Task<TelegramKeyboardMenuBase> UpdateSubscription ( )
		{
			TelegramBot.Data.AddOrUpdate ( Subscription );

			await SendTextBlockAsync ( Subscription.Summary ( ) );

			return this;
		}

		private async Task<TelegramKeyboardMenuBase> ChangeSilenceModeHandler ( CallbackQuery query )
		{
			Subscription.IsSilent = await ReadBoolAsync ( "Keep Silent?" ) ?? false;

			return await UpdateSubscription ( );
		}

		private async Task<TelegramKeyboardMenuBase> ChangeThresholdHandler ( CallbackQuery query )
		{
			await RequestReplyAsync ( "Enter the threshold%" );

			var threshold = await ReadPercentage ( );
			if ( threshold == -1 )
				return this;

			Subscription.Threshold = threshold;

			return await UpdateSubscription ( );
		}

		private async Task<TelegramKeyboardMenuBase> AddSymbolsHandler ( CallbackQuery query )
		{
			var symbols = await ReadSymbolsAsync ( );
			Subscription.AddSymbols ( symbols );

			return await UpdateSubscription ( );
		}

		private async Task<TelegramKeyboardMenuBase> RemoveSymbolsHandler ( CallbackQuery query )
		{
			var symbols = await ReadSymbolsAsync ( );
			Subscription.RemoveSymbols ( symbols );

			return await UpdateSubscription ( );
		}

		private async Task<TelegramKeyboardMenuBase> DeleteHandler ( CallbackQuery query )
		{
			Subscription.Stop ( );
			TelegramBot.Data.PercentChangeSubscriptions.Remove ( Subscription );

			await SendTextBlockAsync ( $"Removed :\n\n{Subscription.Summary ( )}" );

			return Parent;
		}
	}
}