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
using Fody;
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

	[ConfigureAwait ( false )]
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

		protected readonly Dictionary<string, string> ButtonPopups =
			new Dictionary<string, string> ( );

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

			return MenuMessage = await SendTextBlockAsync ( title, replyMarkup: Keyboard );
		}

		public async Task DeleteMenu ( )
		{
			try
			{
				if ( MenuMessage != null )
					await Client.DeleteMessageAsync ( Chat, MenuMessage.MessageId, CancellationToken );
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
					                            cancellationToken: CancellationToken );
				return this;
			}

			if ( ButtonPopups.TryGetValue ( query.Data, out var popupMessage ) )
				await Client
					.AnswerCallbackQueryAsync ( query.Id, popupMessage, cancellationToken: CancellationToken );
			else
				await Client
					.AnswerCallbackQueryAsync ( query.Id, cancellationToken: CancellationToken );

			if ( !Handlers.TryGetValue ( query.Data, out var handler ) )
				return this;

			var menu = await handler ( query );

			return await SwitchTo ( menu );
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
			if ( ReferenceEquals ( menu, this ) && LastId == Id )
				return this;

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

		#region Read Message

		protected async Task<Message> ReadMessageAsync ( )
		{
			Message message;
			while ( !messages.TryDequeue ( out message ) )
				await Task.Delay ( 100, CancellationToken );

			return message;
		}

		protected async Task<bool?> ReadBoolAsync ( string text )
		{
			await SendOptionsAsync ( text, new[] {"yes", "no"}, 2 );

			var message = await ReadMessageAsync ( );

			if ( new[] {"true", "y", "yes"}.Contains ( message.Text.ToLower ( ) ) )
				return true;

			if ( new[] {"false", "n", "no"}.Contains ( message.Text.ToLower ( ) ) )
				return false;

			return null;
		}

		protected async Task<decimal> ReadPercentage ( decimal @default = -1 )
		{
			var message = await ReadMessageAsync ( );

			if ( decimal.TryParse ( message.Text.Trim ( '%' ), out var percentage ) )
				return percentage;

			await SendTextBlockAsync ( $"{message.Text} is not a valid percentage value" );

			return @default;
		}

		protected async Task<CryptoExchangeId?> ReadExchangeIdAsync ( IEnumerable<CryptoExchangeId> exchangeIds )
		{
			var exchanges = TelegramBot.Ctb.Exchanges.Keys
				.Intersect ( exchangeIds )
				.Select ( x => x.ToString ( ) );

			await SendOptionsAsync ( "Select Exchange", exchanges, 2 );

			var message = await ReadMessageAsync ( );
			if ( Enums.TryParse ( message.Text, out CryptoExchangeId exchangeId ) )
				return exchangeId;

			await SendTextBlockAsync ( $"{message.Text} is not a known exchange" );

			return null;
		}

		protected async Task<CryptoExchangeId?> ReadExchangeIdAsync ( ) =>
			await ReadExchangeIdAsync ( TelegramBot.Ctb.Exchanges.Keys );

		protected async Task<List<string>> ReadSymbolsAsync ( )
		{
			await RequestReplyAsync ( "Enter the symbols" );

			var message = await ReadMessageAsync ( );

			return message.Text
				.Split ( " ,".ToCharArray ( ), StringSplitOptions.RemoveEmptyEntries )
				.ToList ( );
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