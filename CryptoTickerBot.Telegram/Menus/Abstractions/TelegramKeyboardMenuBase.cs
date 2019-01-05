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
	internal delegate Task<ITelegramKeyboardMenu> QueryHandlerDelegate ( CallbackQuery query );

	internal abstract class TelegramKeyboardMenuBase : ITelegramKeyboardMenu
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public TelegramBot TelegramBot { get; }
		public User User { get; }
		public Chat Chat { get; }
		public string Title { get; }
		public IEnumerable<IEnumerable<string>> Labels { get; protected set; }
		public InlineKeyboardMarkup Keyboard { get; protected set; }
		public ITelegramKeyboardMenu Parent { get; protected set; }

		protected IBot Ctb => TelegramBot.Ctb;
		protected TelegramBotClient Client => TelegramBot.Client;
		protected CancellationToken CancellationToken => TelegramBot.CancellationToken;

		public Message MenuMessage { get; protected set; }
		public int Id => MenuMessage.MessageId;
		public Message LastMessage { get; protected set; }
		public int LastId => LastMessage?.MessageId ?? 0;

		protected readonly Dictionary<string, QueryHandlerDelegate> Handlers =
			new Dictionary<string, QueryHandlerDelegate> ( );

		protected readonly Dictionary<string, string> ButtonPopups =
			new Dictionary<string, string> ( );

		private readonly ConcurrentQueue<Message> messages = new ConcurrentQueue<Message> ( );

		protected TelegramKeyboardMenuBase ( TelegramBot telegramBot,
		                                     User user,
		                                     Chat chat,
		                                     string title,
		                                     IEnumerable<IEnumerable<string>> labels = null,
		                                     ITelegramKeyboardMenu parent = null )
		{
			TelegramBot = telegramBot;
			User        = user;
			Chat        = chat;
			Title       = title;
			Parent      = parent;
			Labels      = labels?.Select ( x => x.ToList ( ) ).ToList ( );

			BuildKeyboard ( );
		}

		public void SetParentMenu ( ITelegramKeyboardMenu menu )
		{
			Parent = menu;
		}

		public bool Contains ( string label,
		                       StringComparison comparison = StringComparison.OrdinalIgnoreCase ) =>
			Labels.Any ( x => x.Any ( y => y.Equals ( label, comparison ) ) );

		public virtual async Task<Message> DisplayAsync ( )
		{
			var title = Chat.Type == ChatType.Private ? Title : $"{User}:\n{Title}";

			return MenuMessage = await SendTextBlockAsync ( title, replyMarkup: Keyboard ).ConfigureAwait ( false );
		}

		public async Task DeleteMenuAsync ( )
		{
			try
			{
				if ( MenuMessage != null )
					await Client.DeleteMessageAsync ( Chat, MenuMessage.MessageId, CancellationToken )
						.ConfigureAwait ( false );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		public virtual async Task<ITelegramKeyboardMenu> HandleQueryAsync ( CallbackQuery query )
		{
			if ( query.From != User )
			{
				await Client
					.AnswerCallbackQueryAsync ( query.Id,
					                            "This is not your menu!",
					                            cancellationToken: CancellationToken ).ConfigureAwait ( false );
				return this;
			}

			if ( ButtonPopups.TryGetValue ( query.Data, out var popupMessage ) )
				await Client
					.AnswerCallbackQueryAsync ( query.Id, popupMessage, cancellationToken: CancellationToken )
					.ConfigureAwait ( false );
			else
				await Client
					.AnswerCallbackQueryAsync ( query.Id, cancellationToken: CancellationToken )
					.ConfigureAwait ( false );

			if ( !Handlers.TryGetValue ( query.Data, out var handler ) )
				return this;

			try
			{
				var menu = await handler ( query ).ConfigureAwait ( false );
				return await SwitchToMenuAsync ( menu ).ConfigureAwait ( false );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}

			return await SwitchToMenuAsync ( null ).ConfigureAwait ( false );
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
			var labels = new List<List<string>> ( Labels.Select ( x => x.ToList ( ) ) ) {new List<string> {label}};
			Labels = labels;
		}

		protected void BuildKeyboard ( )
		{
			Keyboard = Labels?.ToInlineKeyboardMarkup ( );
		}

		public virtual async Task<ITelegramKeyboardMenu> SwitchToMenuAsync ( ITelegramKeyboardMenu menu )
		{
			if ( ReferenceEquals ( menu, this ) && LastId == Id )
				return this;

			await DeleteMenuAsync ( ).ConfigureAwait ( false );

			if ( menu != null )
				await menu.DisplayAsync ( ).ConfigureAwait ( false );

			return menu;
		}

		protected QueryHandlerDelegate BackHandler => async query => Parent;

		protected QueryHandlerDelegate DummyHandler => async query => this;

		#region Send Message

		protected async Task<Message> SendTextBlockAsync (
			string text,
			int replyToMessageId = 0,
			bool disableWebPagePreview = false,
			bool disableNotification = true,
			IReplyMarkup replyMarkup = null
		) =>
			LastMessage = await Client
				.SendTextMessageAsync ( Chat,
				                        text.ToMarkdown ( ), ParseMode.Markdown,
				                        disableWebPagePreview, disableNotification,
				                        replyToMessageId,
				                        replyMarkup,
				                        CancellationToken )
				.ConfigureAwait ( false );

		protected async Task<Message> EditTextBlockAsync (
			string text,
			bool disableWebPagePreview = false,
			InlineKeyboardMarkup markup = null
		) =>
			LastMessage = await Client
				.EditMessageTextAsync ( Chat.Id,
				                        Parent.Id,
				                        text.ToMarkdown ( ), ParseMode.Markdown,
				                        disableWebPagePreview,
				                        markup,
				                        CancellationToken )
				.ConfigureAwait ( false );

		protected async Task<Message> RequestReplyAsync (
			string text,
			bool disableWebPagePreview = false,
			bool disableNotification = true
		) =>
			LastMessage = await Client
				.SendTextMessageAsync ( Chat,
				                        text.ToMarkdown ( ), ParseMode.Markdown,
				                        disableWebPagePreview, disableNotification,
				                        0,
				                        new ForceReplyMarkup ( ),
				                        CancellationToken )
				.ConfigureAwait ( false );

		protected async Task<Message> SendOptionsAsync<T> ( string text,
		                                                    IEnumerable<T> options,
		                                                    int batchSize = 2 ) =>
			LastMessage = await Client
				.SendOptionsAsync ( Chat, User, text, options.Batch ( batchSize ), CancellationToken )
				.ConfigureAwait ( false );

		#endregion

		#region Read Message

		protected async Task<Message> ReadMessageAsync ( )
		{
			ClearMessageQueue ( );

			Message message;
			while ( !messages.TryDequeue ( out message ) )
				await Task.Delay ( 100, CancellationToken ).ConfigureAwait ( false );

			return message;
		}

		protected async Task<bool?> ReadBoolAsync ( string text )
		{
			await SendOptionsAsync ( text, new[] {"yes", "no"} ).ConfigureAwait ( false );

			var message = await ReadMessageAsync ( ).ConfigureAwait ( false );

			if ( new[] {"true", "y", "yes"}.Contains ( message.Text.ToLower ( ) ) )
				return true;

			if ( new[] {"false", "n", "no"}.Contains ( message.Text.ToLower ( ) ) )
				return false;

			return null;
		}

		protected async Task<decimal> ReadPercentageAsync ( decimal @default = -1 )
		{
			var message = await ReadMessageAsync ( ).ConfigureAwait ( false );

			if ( decimal.TryParse ( message.Text.Trim ( '%' ), out var percentage ) )
				return percentage / 100m;

			await SendTextBlockAsync ( $"{message.Text} is not a valid percentage value" ).ConfigureAwait ( false );

			return @default;
		}

		protected async Task<CryptoExchangeId?> ReadExchangeIdAsync ( IEnumerable<CryptoExchangeId> exchangeIds )
		{
			var exchanges = TelegramBot.Ctb.Exchanges.Keys
				.Intersect ( exchangeIds )
				.Select ( x => x.ToString ( ) );

			await SendOptionsAsync ( "Select Exchange", exchanges ).ConfigureAwait ( false );

			var message = await ReadMessageAsync ( ).ConfigureAwait ( false );
			if ( Enums.TryParse ( message.Text, out CryptoExchangeId exchangeId ) )
				return exchangeId;

			await SendTextBlockAsync ( $"{message.Text} is not a known exchange" ).ConfigureAwait ( false );

			return null;
		}

		protected async Task<CryptoExchangeId?> ReadExchangeIdAsync ( ) =>
			await ReadExchangeIdAsync ( TelegramBot.Ctb.Exchanges.Keys ).ConfigureAwait ( false );

		protected async Task<List<string>> ReadSymbolsAsync ( )
		{
			await RequestReplyAsync ( "Enter the symbols" ).ConfigureAwait ( false );

			var message = await ReadMessageAsync ( ).ConfigureAwait ( false );

			return message.Text
				.Split ( " ,".ToCharArray ( ), StringSplitOptions.RemoveEmptyEntries )
				.ToList ( );
		}

		protected void ClearMessageQueue ( )
		{
			while ( messages.TryDequeue ( out _ ) )
			{
				// Just clearing the queue
			}
		}

		#endregion
	}
}