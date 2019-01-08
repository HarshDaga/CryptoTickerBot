using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Telegram.Extensions;
using CryptoTickerBot.Telegram.Interfaces;
using CryptoTickerBot.Telegram.Menus.Abstractions;
using MoreLinq;
using Telegram.Bot.Types;

#pragma warning disable 1998

namespace CryptoTickerBot.Telegram.Menus.Pages
{
	internal class MainPage : PageBase
	{
		public MainPage ( IMenu menu ) :
			base ( "Main Menu", menu )
		{
			Labels = new[] {"status", "exchange info", "manage subscriptions"}
				.Batch ( 2 )
				.ToList ( );
			AddWideLabel ( "exit" );

			BuildKeyboard ( );
			AddHandlers ( );
		}

		private void AddHandlers ( )
		{
			AddHandler ( "status", StatusHandlerAsync );
			AddHandler ( "exchange info", ExchangeInfoHandlerAsync );
			AddHandler ( "manage subscriptions", ManageSubscriptionsHandlerAsync );
			AddHandler ( "exit", ExitHandler, "Cya!" );
		}

		private async Task StatusHandlerAsync ( CallbackQuery query )
		{
			await Menu.SendTextBlockAsync ( TelegramBot.GetStatusString ( ) ).ConfigureAwait ( false );
			await RedrawAsync ( ).ConfigureAwait ( false );
		}

		private async Task ExchangeInfoHandlerAsync ( CallbackQuery query )
		{
			var id = await RunExchangeSelectionPageAsync ( ).ConfigureAwait ( false );

			if ( id )
			{
				Ctb.TryGetExchange ( id, out var exchange );

				await Menu.SendTextBlockAsync ( exchange.GetSummary ( ) )
					.ConfigureAwait ( false );
				await RedrawAsync ( ).ConfigureAwait ( false );
			}
		}

		private async Task ManageSubscriptionsHandlerAsync ( CallbackQuery query )
		{
			await Menu.SwitchPageAsync ( new ManageSubscriptionsPage ( Menu, this ) ).ConfigureAwait ( false );
		}
	}
}