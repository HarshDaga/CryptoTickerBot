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
			Handlers["change silence mode"] = ChangeSilenceModeHandlerAsync;
			Handlers["change threshold"]    = ChangeThresholdHandlerAsync;
			Handlers["add symbols"]         = AddSymbolsHandlerAsync;
			Handlers["remove symbols"]      = RemoveSymbolsHandlerAsync;
			Handlers["delete"]              = DeleteHandlerAsync;
			Handlers["back"]                = BackHandlerAsync;
		}

		private async Task<TelegramKeyboardMenuBase> UpdateSubscriptionAsync ( )
		{
			TelegramBot.Data.AddOrUpdate ( Subscription );

			await SendTextBlockAsync ( Subscription.Summary ( ) ).ConfigureAwait ( false );

			return this;
		}

		private async Task<TelegramKeyboardMenuBase> ChangeSilenceModeHandlerAsync ( CallbackQuery query )
		{
			Subscription.IsSilent = await ReadBoolAsync ( "Keep Silent?" ).ConfigureAwait ( false ) ?? false;

			return await UpdateSubscriptionAsync ( ).ConfigureAwait ( false );
		}

		private async Task<TelegramKeyboardMenuBase> ChangeThresholdHandlerAsync ( CallbackQuery query )
		{
			await RequestReplyAsync ( "Enter the threshold%" ).ConfigureAwait ( false );

			var threshold = await ReadPercentageAsync ( ).ConfigureAwait ( false );
			if ( threshold == -1 )
				return this;

			Subscription.Threshold = threshold;

			return await UpdateSubscriptionAsync ( ).ConfigureAwait ( false );
		}

		private async Task<TelegramKeyboardMenuBase> AddSymbolsHandlerAsync ( CallbackQuery query )
		{
			var symbols = await ReadSymbolsAsync ( ).ConfigureAwait ( false );
			Subscription.AddSymbols ( symbols );

			return await UpdateSubscriptionAsync ( ).ConfigureAwait ( false );
		}

		private async Task<TelegramKeyboardMenuBase> RemoveSymbolsHandlerAsync ( CallbackQuery query )
		{
			var symbols = await ReadSymbolsAsync ( ).ConfigureAwait ( false );
			Subscription.RemoveSymbols ( symbols );

			return await UpdateSubscriptionAsync ( ).ConfigureAwait ( false );
		}

		private async Task<TelegramKeyboardMenuBase> DeleteHandlerAsync ( CallbackQuery query )
		{
			Subscription.Stop ( );
			TelegramBot.Data.PercentChangeSubscriptions.Remove ( Subscription );

			await SendTextBlockAsync ( $"Removed :\n\n{Subscription.Summary ( )}" ).ConfigureAwait ( false );

			return Parent;
		}
	}
}