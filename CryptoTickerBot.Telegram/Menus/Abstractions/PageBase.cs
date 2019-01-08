using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Telegram.Extensions;
using CryptoTickerBot.Telegram.Interfaces;
using CryptoTickerBot.Telegram.Menus.Pages;
using MoreLinq;
using Nito.AsyncEx;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CryptoTickerBot.Telegram.Menus.Abstractions
{
	public delegate Task QueryHandlerDelegate ( CallbackQuery query );

	internal abstract class PageBase : IPage
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public string Title { get; protected set; }
		public IEnumerable<IEnumerable<string>> Labels { get; protected set; }
		public InlineKeyboardMarkup Keyboard { get; protected set; }
		public IPage PreviousPage { get; protected set; }
		public IMenu Menu { get; }

		protected User User => Menu.User;
		protected Chat Chat => Menu.Chat;
		protected IBot Ctb => Menu.TelegramBot.Ctb;
		protected TelegramBotClient Client => Menu.TelegramBot.Client;
		protected TelegramBot TelegramBot => Menu.TelegramBot;
		protected CancellationToken CancellationToken => Menu.TelegramBot.CancellationToken;

		protected static QueryHandlerDelegate DummyHandler => query => Task.CompletedTask;

		protected QueryHandlerDelegate BackHandler =>
			async query =>
				await GoBackAsync ( ).ConfigureAwait ( false );

		protected QueryHandlerDelegate ExitHandler =>
			async query =>
				await Menu.DeleteAsync ( ).ConfigureAwait ( false );

		protected readonly Dictionary<string, QueryHandlerDelegate> Handlers =
			new Dictionary<string, QueryHandlerDelegate> ( );

		protected readonly Dictionary<string, string> ButtonPopups =
			new Dictionary<string, string> ( );

		protected readonly AsyncAutoResetEvent ButtonPressResetEvent = new AsyncAutoResetEvent ( false );

		protected PageBase ( string title,
		                     IMenu menu,
		                     IEnumerable<IEnumerable<string>> labels = null,
		                     IPage previousPage = null )
		{
			Title = menu.Chat.Type == ChatType.Private ? title : $"{menu.User}:\n{title}";
			Menu  = menu;

			Labels       = labels?.Select ( x => x.ToList ( ) ).ToList ( );
			PreviousPage = previousPage;

			BuildKeyboard ( );
		}

		public virtual Task HandleMessageAsync ( Message message ) =>
			Task.CompletedTask;

		public async Task HandleQueryAsync ( CallbackQuery query )
		{
			ButtonPopups.TryGetValue ( query.Data, out var popupMessage );
			await Client
				.AnswerCallbackQueryAsync ( query.Id, popupMessage, cancellationToken: CancellationToken )
				.ConfigureAwait ( false );

			if ( !Handlers.TryGetValue ( query.Data, out var handler ) )
				return;

			try
			{
				await handler ( query ).ConfigureAwait ( false );
				ButtonPressResetEvent.Set ( );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		public async Task WaitForButtonPressAsync ( ) =>
			await ButtonPressResetEvent.WaitAsync ( CancellationToken ).ConfigureAwait ( false );

		protected async Task GoBackAsync ( ) =>
			await Menu.SwitchPageAsync ( PreviousPage ).ConfigureAwait ( false );

		protected async Task<SelectionPage<T>.SelectionResult> RunSelectionPageAsync<T> (
			IEnumerable<IEnumerable<T>> rows,
			string title = "Choose",
			Func<T, string> toStringConverter = null
		)
		{
			var page = new SelectionPage<T> ( Menu, rows, this, title, toStringConverter );
			var result = await page.DisplayAndWaitAsync ( ).ConfigureAwait ( false );

			if ( !result )
				await Menu.SwitchPageAsync ( this ).ConfigureAwait ( false );
			return result;
		}

		protected async Task<SelectionPage<CryptoExchangeId>.SelectionResult> RunExchangeSelectionPageAsync ( ) =>
			await RunSelectionPageAsync ( Ctb.Exchanges.Keys.Batch ( 2 ),
			                              "Choose an exchange:" )
				.ConfigureAwait ( false );

		protected void AddHandler ( string label,
		                            QueryHandlerDelegate handler,
		                            string popup = null )
		{
			Handlers[label]     = handler;
			ButtonPopups[label] = popup;
		}

		protected async Task RedrawAsync ( )
		{
			await Menu.SwitchPageAsync ( this ).ConfigureAwait ( false );
		}

		protected void AddWideLabel ( string label )
		{
			var labels = new List<List<string>> ( Labels.Select ( x => x.ToList ( ) ) ) {new List<string> {label}};
			Labels = labels;
		}

		protected void BuildKeyboard ( )
		{
			Keyboard = Labels?.ToInlineKeyboardMarkup ( );
		}

		protected async Task<decimal?> ReadPercentageAsync ( )
		{
			var message = await Menu.WaitForMessageAsync ( ).ConfigureAwait ( false );

			if ( decimal.TryParse ( message.Text.Trim ( '%' ), out var percentage ) )
				return percentage / 100m;

			await Menu.SendTextBlockAsync ( $"{message.Text} is not a valid percentage value" )
				.ConfigureAwait ( false );

			return null;
		}

		protected async Task<List<string>> ReadSymbolsAsync ( )
		{
			await Menu.RequestReplyAsync ( "Enter the symbols" ).ConfigureAwait ( false );

			var message = await Menu.WaitForMessageAsync ( ).ConfigureAwait ( false );

			return message.Text
				.Split ( " ,".ToCharArray ( ), StringSplitOptions.RemoveEmptyEntries )
				.ToList ( );
		}

		public override string ToString ( ) => $"{Menu.User} {Title}";
	}
}