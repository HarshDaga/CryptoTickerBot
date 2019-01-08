using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CryptoTickerBot.Telegram.Interfaces
{
	public interface IMenu
	{
		User User { get; }
		Chat Chat { get; }
		TelegramBot TelegramBot { get; }
		IPage CurrentPage { get; }
		Message LastMessage { get; }
		bool IsOpen { get; }

		Task DeleteAsync ( );
		Task<Message> DisplayAsync ( IPage page );

		Task SwitchPageAsync ( IPage page,
		                       bool replaceOld = false );

		Task HandleMessageAsync ( Message message );
		Task HandleQueryAsync ( CallbackQuery query );

		Task<Message> SendTextBlockAsync (
			string text,
			int replyToMessageId = 0,
			bool disableWebPagePreview = false,
			bool disableNotification = true,
			IReplyMarkup replyMarkup = null
		);

		Task<Message> EditTextBlockAsync (
			int messageId,
			string text,
			InlineKeyboardMarkup markup = null
		);

		Task<Message> RequestReplyAsync (
			string text,
			bool disableWebPagePreview = false,
			bool disableNotification = true
		);

		Task<Message> WaitForMessageAsync ( );
	}
}