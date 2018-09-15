using Humanizer;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace CryptoTickerBot.Telegram.Extensions
{
	public static class StringExtensions
	{
		public static string ToMarkdown ( this string str ) =>
			$"```\n{str.Truncate ( 4000 )}\n```";

		public static InputTextMessageContent ToMarkdownMessage ( this string str ) =>
			new InputTextMessageContent ( $"```\n{str.Truncate ( 4000 )}\n```" ) {ParseMode = ParseMode.Markdown};

		public static InlineKeyboardButton ToKeyboardButton ( this string label ) =>
			new InlineKeyboardButton
			{
				Text         = label.Titleize ( ),
				CallbackData = label
			};
	}
}