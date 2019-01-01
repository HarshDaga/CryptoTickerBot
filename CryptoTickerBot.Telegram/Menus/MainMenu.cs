using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Telegram.Extensions;
using CryptoTickerBot.Telegram.Menus.Abstractions;
using MoreLinq;
using Telegram.Bot.Types;

#pragma warning disable 1998

namespace CryptoTickerBot.Telegram.Menus
{
	internal class MainMenu : TelegramKeyboardMenuBase
	{
		public MainMenu ( TelegramBot telegramBot,
		                  User user,
		                  Chat chat ) :
			base ( telegramBot, user, chat, "Main Menu" )
		{
			Labels = new[] {"status", "exchange info", "manage subscriptions"}
				.Batch ( 2 )
				.ToList ( );
			AddWideLabel ( "exit" );

			BuildKeyboard ( );
			AddHandlers ( );

			ButtonPopups["exit"] = "Cya!";
		}

		private void AddHandlers ( )
		{
			Handlers["status"]               = StatusHandlerAsync;
			Handlers["exchange info"]        = ExchangeInfoHandlerAsync;
			Handlers["manage subscriptions"] = ManageSubscriptionsHandlerAsync;
			Handlers["exit"]                 = BackHandler;
		}

		private async Task<ITelegramKeyboardMenu> StatusHandlerAsync ( CallbackQuery query )
		{
			await SendTextBlockAsync ( TelegramBot.GetStatusString ( ) ).ConfigureAwait ( false );

			return this;
		}

		private async Task<ITelegramKeyboardMenu> ExchangeInfoHandlerAsync ( CallbackQuery query )
		{
			var exchangeId = await ReadExchangeIdAsync ( ).ConfigureAwait ( false );
			if ( exchangeId is null )
				return this;

			Ctb.TryGetExchange ( exchangeId.Value, out var exchange );

			await SendTextBlockAsync ( exchange.GetSummary ( ) ).ConfigureAwait ( false );

			return this;
		}

		private async Task<ITelegramKeyboardMenu> ManageSubscriptionsHandlerAsync ( CallbackQuery query ) =>
			new ManageSubscriptionsMenu ( TelegramBot, User, Chat, this );
	}
}