using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Telegram.Interfaces;
using CryptoTickerBot.Telegram.Menus.Abstractions;
using CryptoTickerBot.Telegram.Subscriptions;
using MoreLinq;
using Telegram.Bot.Types;

#pragma warning disable 1998

namespace CryptoTickerBot.Telegram.Menus.Pages
{
	internal class ManageSubscriptionsPage : PageBase
	{
		public ManageSubscriptionsPage ( IMenu menu,
		                                 IPage previousPage ) :
			base ( "Manage Subscriptions", menu, previousPage: previousPage )
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
			AddHandler ( "add subscription", AddSubscriptionHandlerAsync );
			AddHandler ( "edit subscription", EditSubscriptionHandlerAsync );
			AddHandler ( "back", BackHandler );
		}

		private async Task AddSubscriptionHandlerAsync ( CallbackQuery query )
		{
			var exchangeId = await RunExchangeSelectionPageAsync ( ).ConfigureAwait ( false );
			if ( !exchangeId )
				return;

			await Menu.RequestReplyAsync ( "Enter the threshold%" ).ConfigureAwait ( false );
			var threshold = await ReadPercentageAsync ( ).ConfigureAwait ( false );
			if ( threshold is null )
				return;

			var isSilent = await RunSelectionPageAsync ( new[] {"yes", "no"}.Batch ( 2 ), "Keep Silent?" )
				.ConfigureAwait ( false );
			if ( !isSilent )
				return;

			var symbols = await ReadSymbolsAsync ( ).ConfigureAwait ( false );

			var subscription = new TelegramPercentChangeSubscription (
				Chat,
				User,
				exchangeId.Result,
				threshold.Value,
				isSilent.Result == "yes",
				symbols
			);

			await TelegramBot.AddOrUpdateSubscriptionAsync ( subscription ).ConfigureAwait ( false );
			await RedrawAsync ( ).ConfigureAwait ( false );
		}

		private async Task EditSubscriptionHandlerAsync ( CallbackQuery query )
		{
			var subscriptions = TelegramBot.Data.PercentChangeSubscriptions
				.Where ( x => x.ChatId.Identifier == Chat.Id && x.User == User )
				.ToList ( );

			if ( !subscriptions.Any ( ) )
			{
				await Menu.SendTextBlockAsync ( "There are no subscriptions to edit" ).ConfigureAwait ( false );
				return;
			}

			var exchangeId = await RunExchangeSelectionPageAsync ( ).ConfigureAwait ( false );
			if ( !exchangeId )
				return;

			subscriptions = subscriptions.Where ( x => x.ExchangeId == exchangeId ).ToList ( );

			if ( subscriptions.Count == 1 )
			{
				await Menu.SwitchPageAsync ( new EditSubscriptionPage ( Menu, subscriptions[0], this ) )
					.ConfigureAwait ( false );
				return;
			}

			var threshold =
				await RunSelectionPageAsync ( subscriptions.Select ( x => x.Threshold ).Batch ( 2 ),
				                              "Select Threshold:",
				                              p => $"{p:P}" )
					.ConfigureAwait ( false );
			if ( !threshold )
				return;

			var subscription = subscriptions.SingleOrDefault ( x => x.Threshold == threshold );
			if ( subscription is null )
				return;

			await Menu.SwitchPageAsync ( new EditSubscriptionPage ( Menu, subscription, this ) )
				.ConfigureAwait ( false );
		}
	}
}