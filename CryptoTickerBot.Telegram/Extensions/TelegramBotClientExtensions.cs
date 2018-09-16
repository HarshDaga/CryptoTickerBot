using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Extensions;
using MoreLinq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CryptoTickerBot.Telegram.Extensions
{
	public static class TelegramBotClientExtensions
	{
		public static async Task<Message> SendTextBlockAsync ( this TelegramBotClient client,
		                                                       ChatId chatId,
		                                                       string text,
		                                                       bool disableWebPagePreview = false,
		                                                       bool disableNotification = false,
		                                                       int replyToMessageId = 0,
		                                                       IReplyMarkup replyMarkup = null,
		                                                       CancellationToken cancellationToken = default ) =>
			await client.SendTextMessageAsync ( chatId,
			                                    text.ToMarkdown ( ), ParseMode.Markdown,
			                                    disableWebPagePreview, disableNotification,
			                                    replyToMessageId,
			                                    replyMarkup,
			                                    cancellationToken );

		public static async Task SendTextBlocksAsync ( this TelegramBotClient client,
		                                               ChatId chatId,
		                                               string text,
		                                               bool disableWebPagePreview = false,
		                                               bool disableNotification = false,
		                                               int replyToMessageId = 0,
		                                               IReplyMarkup replyMarkup = null,
		                                               CancellationToken cancellationToken = default )
		{
			foreach ( var chunk in text.SplitOnLength ( 4000 ) )
				await client.SendTextMessageAsync ( chatId,
				                                    chunk.ToMarkdown ( ), ParseMode.Markdown,
				                                    disableWebPagePreview, disableNotification,
				                                    replyToMessageId,
				                                    replyMarkup,
				                                    cancellationToken );
		}

		public static async Task<Message> SendOptionsAsync<T> ( this TelegramBotClient client,
		                                                        ChatId chatId,
		                                                        string text,
		                                                        IEnumerable<T> options,
		                                                        CancellationToken cancellationToken = default )
		{
			var keyboard = new ReplyKeyboardMarkup (
				options.Select ( x => new KeyboardButton ( x.ToString ( ) ) ),
				true, true
			);

			return await client.SendTextMessageAsync ( chatId,
			                                           text,
			                                           disableNotification: true,
			                                           replyMarkup: keyboard,
			                                           cancellationToken: cancellationToken );
		}

		public static async Task<Message> SendOptionsAsync<T> ( this TelegramBotClient client,
		                                                        ChatId chatId,
		                                                        string text,
		                                                        IEnumerable<T> options,
		                                                        int batchSize,
		                                                        CancellationToken cancellationToken = default ) =>
			await client.SendOptionsAsync ( chatId, text, options.Batch ( 2 ), cancellationToken );

		public static async Task<Message> SendOptionsAsync<T> ( this TelegramBotClient client,
		                                                        ChatId chatId,
		                                                        string text,
		                                                        IEnumerable<IEnumerable<T>> options,
		                                                        CancellationToken cancellationToken = default )
		{
			var keyboard = new ReplyKeyboardMarkup (
				options.Select ( x => x.Select ( y => new KeyboardButton ( y.ToString ( ) ) ) ),
				true, true );
			return await client.SendTextMessageAsync ( chatId,
			                                           text,
			                                           disableNotification: true,
			                                           replyMarkup: keyboard,
			                                           cancellationToken: cancellationToken );
		}

		public static async Task<Message> SendOptionsAsync<T> ( this TelegramBotClient client,
		                                                        ChatId chatId,
		                                                        User user,
		                                                        string text,
		                                                        IEnumerable<IEnumerable<T>> options,
		                                                        CancellationToken cancellationToken = default )
		{
			var keyboard = new ReplyKeyboardMarkup (
					options.Select ( x => x.Select ( y => new KeyboardButton ( y.ToString ( ) ) ) ),
					true, true )
				{Selective = true};
			return await client.SendTextMessageAsync ( chatId,
			                                           $"{user}\n{text}",
													   disableNotification: true,
			                                           replyMarkup: keyboard,
			                                           cancellationToken: cancellationToken );
		}
	}
}