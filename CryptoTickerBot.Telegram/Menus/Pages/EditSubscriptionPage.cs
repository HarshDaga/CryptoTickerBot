using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Telegram.Interfaces;
using CryptoTickerBot.Telegram.Menus.Abstractions;
using CryptoTickerBot.Telegram.Subscriptions;
using MoreLinq.Extensions;
using Telegram.Bot.Types;

namespace CryptoTickerBot.Telegram.Menus.Pages
{
	internal class EditSubscriptionPage : PageBase
	{
		public TelegramPercentChangeSubscription Subscription { get; }

		public EditSubscriptionPage ( IMenu menu,
		                              TelegramPercentChangeSubscription subscription,
		                              IPage previousPage ) :
			base ( "Edit Subscription", menu, previousPage: previousPage )
		{
			Subscription = subscription;
			Title        = $"Edit Subscription:\n{subscription.Summary ( )}";

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
			Handlers["back"]                = BackHandler;
		}

		private async Task UpdateSubscriptionAsync ( )
		{
			TelegramBot.Data.AddOrUpdate ( Subscription );
			await Menu.SendTextBlockAsync ( Subscription.Summary ( ) ).ConfigureAwait ( false );
		}

		private async Task ChangeSilenceModeHandlerAsync ( CallbackQuery query )
		{
			var isSilent = await RunSelectionPageAsync ( new[] {"Yes", "No"}.Batch ( 2 ), "Keep Silent?" )
				.ConfigureAwait ( false );
			if ( !isSilent )
				return;

			Subscription.IsSilent = isSilent.Result == "Yes";

			await UpdateSubscriptionAsync ( ).ConfigureAwait ( false );
		}

		private async Task ChangeThresholdHandlerAsync ( CallbackQuery query )
		{
			await Menu.RequestReplyAsync ( "Enter the threshold%" ).ConfigureAwait ( false );
			var threshold = await ReadPercentageAsync ( ).ConfigureAwait ( false );
			if ( threshold is null )
				return;

			Subscription.Threshold = threshold.Value;
			await UpdateSubscriptionAsync ( ).ConfigureAwait ( false );
		}

		private async Task AddSymbolsHandlerAsync ( CallbackQuery query )
		{
			var symbols = await ReadSymbolsAsync ( ).ConfigureAwait ( false );
			Subscription.AddSymbols ( symbols );

			await UpdateSubscriptionAsync ( ).ConfigureAwait ( false );
		}

		private async Task RemoveSymbolsHandlerAsync ( CallbackQuery query )
		{
			var symbols = await ReadSymbolsAsync ( ).ConfigureAwait ( false );
			Subscription.RemoveSymbols ( symbols );

			await UpdateSubscriptionAsync ( ).ConfigureAwait ( false );
		}

		private async Task DeleteHandlerAsync ( CallbackQuery query )
		{
			Subscription.Stop ( );
			TelegramBot.Data.PercentChangeSubscriptions.Remove ( Subscription );

			await Menu.SendTextBlockAsync ( $"Removed :\n\n{Subscription.Summary ( )}" ).ConfigureAwait ( false );
		}
	}
}