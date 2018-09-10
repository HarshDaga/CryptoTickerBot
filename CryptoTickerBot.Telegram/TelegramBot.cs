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

		private readonly IDictionary<User, TelegramKeyboardMenu> menuStates =
			new ConcurrentDictionary<User, TelegramKeyboardMenu> ( );

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
						["/menu"] = HandleMenu
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

		private async void OnCallbackQuery ( object sender,
		                                     CallbackQueryEventArgs callbackQueryEventArgs )
		{
			var query = callbackQueryEventArgs.CallbackQuery;

			if ( !menuStates.TryGetValue ( query.From, out var menu ) )
				return;

			if ( menu != null )
				menuStates[query.From] = await menu.HandleQueryAsync ( query ).ConfigureAwait ( false );
		}

		public void Stop ( )
		{
			Client.StopReceiving ( );
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

				ParseMessage( message, out var command, out var parameters );
				Logger.Debug ( $"Received from {message.From} : {command} {parameters.Join( ", " )}" );

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

		private async Task<bool> MenuTextInput ( Message message )
		{
			if ( menuStates.TryGetValue ( message.From, out var menu ) && menu != null )
			{
				menuStates[message.From] = await menu.HandleMessageAsync ( message ).ConfigureAwait ( false );

				return true;
			}

			return false;
		}

		private async Task HandleMenu ( Message message )
		{
			var from = message.From;

			if ( message.Chat.Type != ChatType.Private )
			{
				await Client
					.SendTextBlockAsync ( message.Chat,
					                      "This can only be done from a private chat.",
					                      cancellationToken: Ctb.Cts.Token )
					.ConfigureAwait ( false );
				return;
			}

			menuStates.TryGetValue ( message.From, out var menu );
			if ( menu != null )
				await menu.DeleteMenu ( ).ConfigureAwait ( false );

			menu = new MainMenu ( this, from, message.Chat );
			await menu.Display ( ).ConfigureAwait ( false );
			menuStates[message.From] = menu;
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
	}
}