using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Extensions;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Domain;
using CryptoTickerBot.Telegram.Extensions;
using CryptoTickerBot.Telegram.Keyboard;
using NLog;
using Polly;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

#pragma warning disable 1998

namespace CryptoTickerBot.Telegram
{
	public delegate Task CommandHandlerDelegate ( Message message );

	public class TelegramBot
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public IBot Ctb { get; }

		public TelegramBotClient Client { get; }
		public User Self { get; private set; }
		public TelegramBotData BotData { get; }
		public BotConfig Config { get; set; }
		public Policy Policy { get; set; }
		public Policy RetryForeverPolicy { get; set; }

		private readonly ImmutableDictionary<string, CommandHandlerDelegate> commandHandlers =
			ImmutableDictionary<string, CommandHandlerDelegate>.Empty;

		private readonly IDictionary<int, TelegramKeyboardMenu> menuStates =
			new ConcurrentDictionary<int, TelegramKeyboardMenu> ( );

		public TelegramBot ( BotConfig config,
		                     IBot ctb )
		{
			Config = config;
			Ctb    = ctb;

			Client                 =  new TelegramBotClient ( Config.BotToken );
			Client.OnMessage       += OnMessage;
			Client.OnInlineQuery   += OnInlineQuery;
			Client.OnCallbackQuery += OnCallbackQuery;
			Client.OnReceiveError  += OnError;

			BotData       =  new TelegramBotData ( );
			BotData.Error += exception => Logger.Error ( exception );

			Policy = Policy
				.Handle<Exception> ( )
				.WaitAndRetryAsync (
					Config.RetryLimit,
					i => Config.RetryInterval,
					( exception,
					  retryCount,
					  span ) =>
					{
						Logger.Error ( exception );
						return Task.CompletedTask;
					}
				);

			commandHandlers = commandHandlers
				.AddRange ( new Dictionary<string, CommandHandlerDelegate>
					{
						["/menu"] = HandleMenuCommand
					}
				);
		}

		public async Task StartAsync ( )
		{
			Logger.Info ( "Starting Telegram Bot" );
			try
			{
				await Policy
					.ExecuteAsync ( async ( ) =>
					{
						Self = await Client.GetMeAsync ( );
						Logger.Info ( $"Hello! My name is {Self.FirstName}" );

						Client.StartReceiving ( );
					} )
					.ConfigureAwait ( false );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
				throw;
			}
		}

		public void Stop ( )
		{
			Client.StopReceiving ( );
		}

		private void UpdateMenuState ( int id,
		                               TelegramKeyboardMenu menu )
		{
			menuStates.Remove ( id );
			if ( menu != null )
				menuStates[menu.Id] = menu;
		}

		private async Task<bool> MenuTextInput ( Message message )
		{
			if ( !menuStates.TryGetValue ( message.MessageId, out var menu ) || menu == null )
				return false;

			var id = menu.Id;
			menu = await menu.HandleMessageAsync ( message ).ConfigureAwait ( false );
			UpdateMenuState ( id, menu );

			return true;
		}

		private async Task CloseExistingKeyboards ( User user )
		{
			foreach ( var menu in menuStates.Values.Where ( x => x.User == user ).ToList ( ) )
			{
				await menu.DeleteMenu ( ).ConfigureAwait ( false );
				menuStates.Remove ( menu.Id );
			}
		}

		private async Task HandleMenuCommand ( Message message )
		{
			var from = message.From;

			await CloseExistingKeyboards ( message.From );

			var menu = new MainMenu ( this, from, message.Chat );
			await menu.Display ( ).ConfigureAwait ( false );
			menuStates[menu.Id] = menu;
		}

		private void ParseMessage ( Message message,
		                            out string command,
		                            out List<string> parameters )
		{
			var text = message.Text;
			command = text.Split ( ' ' ).First ( );
			if ( command.Contains ( $"@{Self.Username}" ) )
				command = command.Substring ( 0, command.IndexOf ( $"@{Self.Username}", StringComparison.Ordinal ) );
			parameters = text
				.Split ( new[] {' '}, StringSplitOptions.RemoveEmptyEntries )
				.Skip ( 1 )
				.ToList ( );
		}

		#region TelegramBotClient Event Handlers

		private async void OnCallbackQuery ( object sender,
		                                     CallbackQueryEventArgs callbackQueryEventArgs )
		{
			var query = callbackQueryEventArgs.CallbackQuery;

			if ( !menuStates.TryGetValue ( query.Message.MessageId, out var menu ) )
			{
				try
				{
					await Client
						.AnswerCallbackQueryAsync ( query.Id,
						                            "Menu was closed!",
						                            cancellationToken: Ctb.Cts.Token )
						.ConfigureAwait ( false );
					await Client.DeleteMessageAsync ( query.Message.Chat, query.Message.MessageId, Ctb.Cts.Token )
						.ConfigureAwait ( false );
				}
				catch ( Exception e )
				{
					Logger.Error ( e );
				}

				return;
			}

			if ( menu == null )
				return;

			menu = await menu.HandleQueryAsync ( query ).ConfigureAwait ( false );
			UpdateMenuState ( query.Message.MessageId, menu );
		}

		private static void OnError ( object sender,
		                              ReceiveErrorEventArgs e )
		{
			Logger.Error (
				e.ApiRequestException,
				$"Error Code: {e.ApiRequestException.ErrorCode}"
			);
		}

		private async void OnInlineQuery ( object sender,
		                                   InlineQueryEventArgs e )
		{
			var from = e.InlineQuery.From;
			Logger.Info ( $"Received inline query from: {from.Id,-10} {from.FirstName}" );
			if ( !BotData.Users.Contains ( from ) )
				BotData.AddUser ( from, UserRole.Guest );

			var words = e.InlineQuery.Query.Split ( new[] {' '}, StringSplitOptions.RemoveEmptyEntries );
			var inlineQueryResults = Ctb.Exchanges.Values
				.Select ( x => x.ToInlineQueryResult ( words ) )
				.ToList ( );

			try
			{
				await Client
					.AnswerInlineQueryAsync (
						e.InlineQuery.Id,
						inlineQueryResults,
						0
					).ConfigureAwait ( false );
			}
			catch ( Exception exception )
			{
				Logger.Error ( exception );
			}
		}

		private async void OnMessage ( object sender,
		                               MessageEventArgs e )
		{
			try
			{
				var message = e.Message;

				if ( message is null || message.Type != MessageType.Text )
					return;

				ParseMessage ( message, out var command, out var parameters );
				Logger.Debug ( $"Received from {message.From} : {command} {parameters.Join ( ", " )}" );

				if ( commandHandlers.TryGetValue ( command, out var handler ) )
				{
					await handler ( message ).ConfigureAwait ( false );
					return;
				}

				await MenuTextInput ( message );
			}
			catch ( Exception exception )
			{
				Logger.Error ( exception );
			}
		}

		#endregion
	}
}