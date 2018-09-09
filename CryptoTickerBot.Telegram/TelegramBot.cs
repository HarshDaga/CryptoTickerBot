using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
	public class TelegramBot
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public IBot Ctb { get; }

		public TelegramBotClient Client { get; }
		public User Me { get; private set; }
		public TelegramBotData BotData { get; }
		public BotConfig Config { get; set; }
		public Policy Policy { get; set; }
		public Policy RetryForeverPolicy { get; set; }

		private readonly IDictionary<User, TelegramKeyboardMenu> userMenuStates =
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
		}

		public async Task StartAsync ( )
		{
			Logger.Info ( "Starting Telegram Bot" );
			try
			{
				await Policy
					.ExecuteAsync ( async ( ) =>
					{
						Me = await Client.GetMeAsync ( );
						Logger.Info ( $"Hello! My name is {Me.FirstName}" );

						Client.StartReceiving ( );
					} );
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

			if ( !userMenuStates.TryGetValue ( query.From, out var menu ) )
				return;

			if ( menu != null )
				userMenuStates[query.From] = await menu.HandleQueryAsync ( query ).ConfigureAwait ( false );
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

				if ( message == null || message.Type != MessageType.Text )
					return;

				var from = message.From;

				if ( await HandleCommand ( message ) )
					return;

				if ( userMenuStates.TryGetValue ( from, out var menu ) && menu != null )
					userMenuStates[from] = await menu.HandleMessageAsync ( message );
			}
			catch ( Exception exception )
			{
				Logger.Error ( exception );
			}
		}

		private async Task<bool> HandleCommand ( Message message )
		{
			var from = message.From;

			ParseMessage ( message, out var command, out var parameters );
			Logger.Debug ( $"Received from {from} : {command} {parameters.Join ( ", " )}" );

			if ( command == "/menu" )
			{
				if ( message.Chat.Type != ChatType.Private )
				{
					await Client.SendTextBlockAsync ( message.Chat,
					                                  "This can only be done from a private chat.",
					                                  cancellationToken: Ctb.Cts.Token );
					return true;
				}

				userMenuStates.TryGetValue ( from, out var menu );
				if ( menu != null )
					await menu.DeleteMenu ( );

				userMenuStates[from] = new MainMenu ( this, from, message.Chat );
				await userMenuStates[from].Display ( );

				return true;
			}

			return false;
		}

		private void ParseMessage ( Message message,
		                            out string command,
		                            out List<string> parameters )
		{
			var text = message.Text;
			command = text.Split ( ' ' ).First ( );
			if ( command.Contains ( $"@{Me.Username}" ) )
				command = command.Substring ( 0, command.IndexOf ( $"@{Me.Username}", StringComparison.Ordinal ) );
			parameters = text
				.Split ( new[] {' '}, StringSplitOptions.RemoveEmptyEntries )
				.Skip ( 1 )
				.ToList ( );
		}
	}
}