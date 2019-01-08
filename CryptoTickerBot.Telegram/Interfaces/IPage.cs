using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CryptoTickerBot.Telegram.Interfaces
{
	public interface IPage
	{
		string Title { get; }
		IEnumerable<IEnumerable<string>> Labels { get; }
		InlineKeyboardMarkup Keyboard { get; }
		IPage PreviousPage { get; }
		IMenu Menu { get; }
		Task HandleMessageAsync ( Message message );
		Task HandleQueryAsync ( CallbackQuery query );
		Task WaitForButtonPressAsync ( );
	}
}