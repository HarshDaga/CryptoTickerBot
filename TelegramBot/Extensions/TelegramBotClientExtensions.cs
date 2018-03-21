using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot.Extensions
{
	public static class TelegramBotClientExtensions
	{
		[DebuggerStepThrough]
		public static async Task<Message> ReplyTextMessageAsync (
			this TelegramBotClient bot, Message message, string text,
			ParseMode parseMode = ParseMode.Default,
			bool disableWebPagePreview = false, bool disableNotification = false,
			int replyToMessageId = 0, IReplyMarkup replyMarkup = null,
			CancellationToken cancellationToken = default ) =>
			await bot.SendTextMessageAsync (
					message.Chat.Id, text, parseMode,
					disableWebPagePreview, disableNotification,
					replyToMessageId, replyMarkup, cancellationToken )
				.ConfigureAwait ( false );
	}
}