using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Telegram.Extensions;
using EnumsNET;
using MoreLinq.Extensions;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

#pragma warning disable 1998

namespace CryptoTickerBot.Telegram.Menus.Abstractions
{
	internal delegate Task<TelegramKeyboardMenuBase> QueryHandlerDelegate ( CallbackQuery query );

	internal abstract class TelegramKeyboardMenuBase : ITelegramKeyboardMenu
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public TelegramBot TelegramBot { get; }
		public User User { get; }
		public Chat Chat { get; }
		public string Title { get; }
		public IEnumerable<IEnumerable<string>> Labels { get; protected set; }
		public InlineKeyboardMarkup Keyboard { get; protected set; }
		public TelegramKeyboardMenuBase Parent { get; protected set; }

		protected IBot Ctb => TelegramBot.Ctb;
		protected TelegramBotClient Client => TelegramBot.Client;
		protected CancellationToken CancellationToken => TelegramBot.CancellationToken;

		public Message MenuMessage { get; protected set; }
		public int Id => MenuMessage.MessageId;
		public Message LastMessage { get; protected set; }
		public int LastId => LastMessage?.MessageId ?? 0;

		protected readonly Dictionary<string, QueryHandlerDelegate> Handlers =
			new Dictionary<string, QueryHandlerDelegate> ( );

		private readonly ConcurrentQueue<Message> messages = new ConcurrentQueue<Message> ( );

		protected TelegramKeyboardMenuBase ( TelegramBot telegramBot,
		                                     User user,
		                                     Chat chat,
		                                     string title,
		                                     IEnumerable<IList<string>> labels = null,
		                                     TelegramKeyboardMenuBase parent = null )
		{
			TelegramBot = telegramBot;
			User        = user;
			Chat        = chat;
			Title       = title;
			Parent      = parent;
			Labels      = labels?.ToList ( );

			BuildKeyboard ( );
		}

		public void SetParentMenu ( TelegramKeyboardMenuBase menu )
		{
			Parent = menu;
		}

		public bool Contains ( string label,
		                       StringComparison comparison = StringComparison.OrdinalIgnoreCase ) =>
			Labels.Any ( x => x.Any ( y => y.Equals ( label, comparison ) ) );

		public async Task<Message> Display ( )
		{
			var title = Chat.Type == ChatType.Private ? Title : $"{User}:\n{Title}";

			return MenuMessage = await
				SendTextBlockAsync ( title, replyMarkup: Keyboard )
					.ConfigureAwait ( false );
		}

		public async Task DeleteMenu ( )
		{
			try
			{
				if ( MenuMessage != null )
					await Client
						.DeleteMessageAsync ( Chat, MenuMessage.MessageId, CancellationToken )
						.ConfigureAwait ( false );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		public virtual async Task<TelegramKeyboardMenuBase> HandleQueryAsync ( CallbackQuery query )
		{
			if ( query.From != User )
			{
				await Client
					.AnswerCallbackQueryAsync ( query.Id,
					                            "This is not your menu!",
					                            cancellationToken: CancellationToken )
					.ConfigureAwait ( false );
				return this;
			}

			await Client
				.AnswerCallbackQueryAsync ( query.Id, cancellationToken: CancellationToken )
				.ConfigureAwait ( false );

			if ( !Handlers.TryGetValue ( query.Data, out var handler ) )
				return this;

			var menu = await handler ( query ).ConfigureAwait ( false );

			return await SwitchTo ( menu ).ConfigureAwait ( false );
		}

		public virtual async Task HandleMessageAsync ( Message message )
		{
			if ( message.From != User )
				return;

			messages.Enqueue ( message );
		}

		public override string ToString ( ) => $"{User} {Title}";

		protected void AddWideLabel ( string label )
		{
			Labels = Enumerable.Append (
					Labels,
					new[] {label}
				)
				.ToList ( );
		}

		protected void BuildKeyboard ( )
		{
			Keyboard = Labels?.ToInlineKeyboardMarkup ( );
		}

		protected async Task<TelegramKeyboardMenuBase> SwitchTo ( TelegramKeyboardMenuBase menu )
		{
			await DeleteMenu ( );

			if ( menu != null )
				await menu.Display ( );

			return menu;
		}

		protected async Task<TelegramKeyboardMenuBase> BackHandler ( CallbackQuery query ) => Parent;

		protected async Task<TelegramKeyboardMenuBase> DummyHandler ( CallbackQuery query ) => this;

		#region Send Message

		protected async Task<Message> SendTextBlockAsync (
			string text,
			int replyToMessageId = 0,
			bool disableWebPagePreview = false,
			bool disableNotification = false,
			IReplyMarkup replyMarkup = null
		) =>
			LastMessage = await Client.SendTextMessageAsync ( Chat,
			                                                  text.ToMarkdown ( ), ParseMode.Markdown,
			                                                  disableWebPagePreview, disableNotification,
			                                                  replyToMessageId,
			                                                  replyMarkup,
			                                                  CancellationToken );

		protected async Task<Message> RequestReplyAsync (
			string text,
			bool disableWebPagePreview = false,
			bool disableNotification = false,
			IReplyMarkup replyMarkup = null
		) =>
			LastMessage = await Client.SendTextMessageAsync ( Chat,
			                                                  text.ToMarkdown ( ), ParseMode.Markdown,
			                                                  disableWebPagePreview, disableNotification,
			                                                  0,
			                                                  new ForceReplyMarkup ( ),
			                                                  CancellationToken );

		protected async Task<Message> SendOptionsAsync<T> ( string text,
		                                                    IEnumerable<T> options,
		                                                    int batchSize ) =>
			LastMessage = await Client.SendOptionsAsync ( Chat, User, text, options.Batch ( 2 ), CancellationToken );

		#endregion

		#region Get Message

		protected async Task<Message> GetMessageAsync ( )
		{
			Message message;
			while ( !messages.TryDequeue ( out message ) )
				await Task.Delay ( 100, CancellationToken );

			return message;
		}

		protected async Task<bool> GetBoolAsync ( string text,
		                                          bool @default = false )
		{
			await SendOptionsAsync ( text, new[] {"yes", "no"}, 2 );

			var message = await GetMessageAsync ( );

			if ( new[] {"true", "y", "yes"}.Contains ( message.Text.ToLower ( ) ) )
				return true;

			if ( new[] {"false", "n", "no"}.Contains ( message.Text.ToLower ( ) ) )
				return false;

			return @default;
		}

		protected async Task<CryptoExchangeId?> GetExchangeIdAsync ( )
		{
			var exchanges = TelegramBot.Ctb.Exchanges.Keys.Select ( x => x.ToString ( ) );

			await SendOptionsAsync ( "Select Exchange", exchanges, 2 );

			var message = await GetMessageAsync ( );
			if ( Enums.TryParse ( message.Text, out CryptoExchangeId exchangeId ) )
				return exchangeId;

			await SendTextBlockAsync ( $"{message.Text} is not a known exchange" );

			return null;
		}

		protected void ClearMessageQueue ( )
		{
			while ( messages.TryDequeue ( out _ ) )
			{
			}
		}

		#endregion
	}
}