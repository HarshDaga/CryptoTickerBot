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
			Handlers["status"]               = StatusHandler;
			Handlers["exchange info"]        = ExchangeInfoHandler;
			Handlers["manage subscriptions"] = ManageSubscriptionsHandler;
			Handlers["exit"]                 = BackHandler;
		}

		private async Task<TelegramKeyboardMenuBase> StatusHandler ( CallbackQuery query )
		{
			await SendTextBlockAsync ( TelegramBot.GetStatusString ( ) );

			return this;
		}

		private async Task<TelegramKeyboardMenuBase> ExchangeInfoHandler ( CallbackQuery query )
		{
			var exchangeId = await ReadExchangeIdAsync ( );
			if ( exchangeId is null )
				return this;

			Ctb.TryGetExchange ( exchangeId.Value, out var exchange );

			await SendTextBlockAsync ( exchange.GetSummary ( ) );

			return this;
		}

		private async Task<TelegramKeyboardMenuBase> ManageSubscriptionsHandler ( CallbackQuery query ) =>
			new ManageSubscriptionsMenu ( TelegramBot, User, Chat, this );
	}
}