using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Telegram.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

#pragma warning disable 1998

namespace CryptoTickerBot.Telegram.Keyboard
{
	internal delegate Task<TelegramKeyboardMenu> QueryHandlerDelegate ( CallbackQuery query );

	internal delegate Task<TelegramKeyboardMenu> MessageHandlerDelegate ( Message message );

	internal class TelegramKeyboardMenu
	{
		public TelegramBot TelegramBot { get; }
		public User User { get; }
		public Chat Chat { get; }
		public string Title { get; }
		public IEnumerable<IEnumerable<string>> Labels { get; protected set; }
		public InlineKeyboardMarkup Keyboard { get; protected set; }
		public TelegramKeyboardMenu Parent { get; protected set; }

		protected IBot Ctb => TelegramBot.Ctb;
		protected TelegramBotClient Client => TelegramBot.Client;
		protected CancellationToken CancellationToken => TelegramBot.Ctb.Cts.Token;

		protected readonly Dictionary<string, QueryHandlerDelegate> Handlers =
			new Dictionary<string, QueryHandlerDelegate> ( );

		protected MessageHandlerDelegate MessageHandler;

		protected Message LastMessageSent;

		protected TelegramKeyboardMenu ( TelegramBot telegramBot,
		                                 User user,
		                                 Chat chat,
		                                 string title,
		                                 IEnumerable<IList<string>> labels = null,
		                                 TelegramKeyboardMenu parent = null )
		{
			TelegramBot = telegramBot;
			User        = user;
			Chat        = chat;
			Title       = title;
			Parent      = parent;
			Labels      = labels?.ToList ( );

			BuildKeyboard ( );
		}

		public override string ToString ( ) => $"{User} {Title}";

		protected void BuildKeyboard ( )
		{
			Keyboard = Labels?.ToInlineKeyboardMarkup ( );
		}

		public void SetParentMenu ( TelegramKeyboardMenu menu )
		{
			Parent = menu;
		}

		public bool Contains ( string label,
		                       StringComparison comparison = StringComparison.OrdinalIgnoreCase ) =>
			Labels.Any ( x => x.Any ( y => y.Equals ( label, comparison ) ) );

		public async Task<Message> Display ( )
		{
			return LastMessageSent = await Client.SendTextMessageAsync (
				Chat,
				Title.ToMarkdown ( ), ParseMode.Markdown,
				replyMarkup: Keyboard,
				cancellationToken: CancellationToken
			).ConfigureAwait ( false );
		}

		public async Task DeleteMenu ( )
		{
			if ( LastMessageSent != null )
				await Client
					.DeleteMessageAsync ( Chat, LastMessageSent.MessageId, CancellationToken )
					.ConfigureAwait ( false );
		}

		protected async Task<TelegramKeyboardMenu> SwitchTo ( TelegramKeyboardMenu menu )
		{
			await DeleteMenu ( ).ConfigureAwait ( false );

			if ( menu != null )
				await menu.Display ( ).ConfigureAwait ( false );

			return menu;
		}

		public virtual async Task<TelegramKeyboardMenu> HandleQueryAsync ( CallbackQuery query )
		{
			if ( query.From != User )
				return this;

			await Client
				.AnswerCallbackQueryAsync ( query.Id, cancellationToken: CancellationToken )
				.ConfigureAwait ( false );

			if ( !Handlers.TryGetValue ( query.Data, out var handler ) )
				return this;

			var menu = await handler ( query ).ConfigureAwait ( false );

			return await SwitchTo ( menu );
		}

		public virtual async Task<TelegramKeyboardMenu> HandleMessageAsync ( Message message )
		{
			if ( message.From != User )
				return this;

			if ( MessageHandler is null )
				return this;

			var handler = MessageHandler;
			MessageHandler = null;

			var menu = await handler ( message ).ConfigureAwait ( false );

			return await SwitchTo ( menu );
		}

		protected async Task<TelegramKeyboardMenu> BackHandler ( CallbackQuery query ) => Parent;
	}
}