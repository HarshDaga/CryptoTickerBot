using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Telegram.Extensions;
using CryptoTickerBot.Telegram.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CryptoTickerBot.Telegram.Menus
{
	internal class TelegramKeyboardMenu : IMenu
	{
		public User User { get; }
		public Chat Chat { get; }
		public TelegramBot TelegramBot { get; }
		public IPage CurrentPage { get; private set; }
		public Message LastMessage { get; private set; }
		public bool IsOpen => LastMessage != null;
		public TelegramBotClient Client => TelegramBot.Client;
		public CancellationToken CancellationToken => TelegramBot.CancellationToken;


		protected readonly ConcurrentQueue<Message> Messages = new ConcurrentQueue<Message> ( );

		public TelegramKeyboardMenu ( User user,
		                              Chat chat,
		                              TelegramBot telegramBot )
		{
			User        = user;
			Chat        = chat;
			TelegramBot = telegramBot;
		}

		public async Task DeleteAsync ( )
		{
			if ( LastMessage != null )
			{
				await Client
					.DeleteMessageAsync ( Chat.Id, LastMessage.MessageId, CancellationToken )
					.ConfigureAwait ( false );
				LastMessage = null;
			}
		}

		public async Task<Message> DisplayAsync ( IPage page )
		{
			await DeleteAsync ( ).ConfigureAwait ( false );
			CurrentPage = page;

			if ( CurrentPage != null )
				return LastMessage = await SendTextBlockAsync ( page.Title, replyMarkup: page.Keyboard )
					.ConfigureAwait ( false );

			return null;
		}

		public async Task SwitchPageAsync ( IPage page,
		                                    bool replaceOld = false )
		{
			CurrentPage = page;

			if ( replaceOld )
			{
				if ( CurrentPage != null )
					LastMessage =
						await EditTextBlockAsync ( LastMessage.MessageId, CurrentPage.Title, CurrentPage.Keyboard )
							.ConfigureAwait ( false );
			}
			else
			{
				await DisplayAsync ( page ).ConfigureAwait ( false );
			}
		}

		public async Task HandleMessageAsync ( Message message )
		{
			if ( message.From == User )
			{
				Messages.Enqueue ( message );
				await CurrentPage.HandleMessageAsync ( message ).ConfigureAwait ( false );
			}
		}

		public async Task HandleQueryAsync ( CallbackQuery query )
		{
			if ( query.From != User )
			{
				await Client
					.AnswerCallbackQueryAsync ( query.Id,
					                            "This is not your menu!",
					                            cancellationToken: CancellationToken ).ConfigureAwait ( false );
				return;
			}

			await CurrentPage.HandleQueryAsync ( query ).ConfigureAwait ( false );
		}

		public async Task<Message> SendTextBlockAsync (
			string text,
			int replyToMessageId = 0,
			bool disableWebPagePreview = false,
			bool disableNotification = true,
			IReplyMarkup replyMarkup = null
		) =>
			await Client
				.SendTextMessageAsync ( Chat,
				                        text.ToMarkdown ( ), ParseMode.Markdown,
				                        disableWebPagePreview, disableNotification,
				                        replyToMessageId,
				                        replyMarkup,
				                        CancellationToken )
				.ConfigureAwait ( false );

		public async Task<Message> EditTextBlockAsync (
			int messageId,
			string text,
			InlineKeyboardMarkup markup = null
		) =>
			await Client
				.EditMessageTextAsync ( Chat.Id,
				                        messageId,
				                        text.ToMarkdown ( ), ParseMode.Markdown,
				                        false,
				                        markup,
				                        CancellationToken )
				.ConfigureAwait ( false );

		public async Task<Message> RequestReplyAsync (
			string text,
			bool disableWebPagePreview = false,
			bool disableNotification = true
		) =>
			await Client
				.SendTextMessageAsync ( Chat,
				                        text.ToMarkdown ( ), ParseMode.Markdown,
				                        disableWebPagePreview, disableNotification,
				                        0,
				                        new ForceReplyMarkup ( ),
				                        CancellationToken )
				.ConfigureAwait ( false );

		public async Task<Message> WaitForMessageAsync ( )
		{
			ClearMessageQueue ( );

			Message message;
			while ( !Messages.TryDequeue ( out message ) )
				await Task.Delay ( 100, CancellationToken ).ConfigureAwait ( false );

			return message;
		}

		protected void ClearMessageQueue ( )
		{
			while ( Messages.TryDequeue ( out _ ) )
			{
				// Just clearing the queue
			}
		}
	}
}