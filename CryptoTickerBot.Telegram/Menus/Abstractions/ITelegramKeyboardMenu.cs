using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CryptoTickerBot.Telegram.Menus.Abstractions
{
	internal interface ITelegramKeyboardMenu
	{
		Chat Chat { get; }
		int Id { get; }
		InlineKeyboardMarkup Keyboard { get; }
		IEnumerable<IEnumerable<string>> Labels { get; }
		int LastId { get; }
		Message LastMessage { get; }
		Message MenuMessage { get; }
		ITelegramKeyboardMenu Parent { get; }
		TelegramBot TelegramBot { get; }
		string Title { get; }
		User User { get; }

		bool Contains ( string label,
		                StringComparison comparison = StringComparison.OrdinalIgnoreCase );

		Task DeleteMenuAsync ( );
		Task<Message> DisplayAsync ( );
		Task<ITelegramKeyboardMenu> SwitchToMenuAsync ( ITelegramKeyboardMenu menu );
		Task HandleMessageAsync ( Message message );
		Task<ITelegramKeyboardMenu> HandleQueryAsync ( CallbackQuery query );
		void SetParentMenu ( ITelegramKeyboardMenu menu );
	}
}