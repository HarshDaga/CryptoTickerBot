using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Extensions;
using CryptoTickerBot.Telegram.Extensions;
using CryptoTickerBot.Telegram.Interfaces;
using CryptoTickerBot.Telegram.Menus;
using JetBrains.Annotations;
using NLog;
using Polly;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CryptoTickerBot.Telegram
{
	public delegate Task CommandHandlerDelegate ( Message message );

	public abstract class TelegramBotBase : ITelegramBot
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public TelegramBotClient Client { get; }
		public User Self { get; protected set; }
		public TelegramBotConfig Config { get; }
		public Policy Policy { get; }
		public DateTime StartTime { get; protected set; }
		public abstract CancellationToken CancellationToken { get; }
		private protected readonly TelegramMenuManager MenuManager;

		protected ImmutableDictionary<string, (string usage, CommandHandlerDelegate handler)> CommandHandlers;

		protected TelegramBotBase ( TelegramBotConfig config )
		{
			Config = config;

			Client = new TelegramBotClient ( Config.BotToken );
			Client.OnMessage += ( _,
			                      args ) => OnMessageInternal ( args.Message );
			Client.OnInlineQuery += ( _,
			                          args ) => OnInlineQuery ( args.InlineQuery );
			Client.OnCallbackQuery += ( _,
			                            args ) => OnCallbackQuery ( args.CallbackQuery );
			Client.OnReceiveError += ( _,
			                           args ) => OnError ( args.ApiRequestException );

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

			CommandHandlers = ImmutableDictionary<string, (string usage, CommandHandlerDelegate handler)>.Empty;
			MenuManager     = new TelegramMenuManager ( );
		}

		public virtual async Task StartAsync ( )
		{
			Logger.Info ( "Starting Telegram Bot" );
			try
			{
				await Policy
					.ExecuteAsync ( async ( ) =>
					{
						StartTime = DateTime.UtcNow;
						Self      = await Client.GetMeAsync ( CancellationToken ).ConfigureAwait ( false );
						Logger.Info ( $"Hello! My name is {Self.FirstName}" );

						Client.StartReceiving ( cancellationToken: CancellationToken );
					} ).ConfigureAwait ( false );

				await OnStartAsync ( ).ConfigureAwait ( false );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
				throw;
			}
		}

		public virtual void Stop ( )
		{
			Client.StopReceiving ( );
		}

		protected void AddCommandHandler ( string command,
		                                   string usage,
		                                   CommandHandlerDelegate handler )
		{
			CommandHandlers = CommandHandlers.Add ( command, ( usage, handler ) );
		}

		protected virtual void OnError ( ApiRequestException exception )
		{
			Logger.Error (
				exception,
				$"Error Code: {exception.ErrorCode}"
			);
		}

		protected virtual async void OnCallbackQuery ( CallbackQuery query )
		{
			var user = query.From;
			var chat = query.Message.Chat;

			try
			{
				if ( query.Message.Date < StartTime )
				{
					await Client
						.AnswerCallbackQueryAsync ( query.Id,
						                            "Menu expired!",
						                            cancellationToken: CancellationToken )
						.ConfigureAwait ( false );
					await Client.DeleteMessageAsync ( query.Message.Chat, query.Message.MessageId, CancellationToken )
						.ConfigureAwait ( false );
				}

				if ( !MenuManager.TryGetMenu ( user, chat.Id, out var menu ) ||
				     menu.LastMessage.MessageId != query.Message.MessageId )
				{
					await Client
						.AnswerCallbackQueryAsync ( query.Id,
						                            "Get your own menu!",
						                            cancellationToken: CancellationToken )
						.ConfigureAwait ( false );
					return;
				}

				await menu.HandleQueryAsync ( query ).ConfigureAwait ( false );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		protected abstract void OnInlineQuery ( InlineQuery query );

		protected virtual async void OnMessageInternal ( Message message )
		{
			try
			{
				if ( message is null || message.Type != MessageType.Text )
					return;

				var (command, parameters) = message.ExtractCommand ( Self );
				Logger.Debug ( $"Received from {message.From} : {command} {parameters.Join ( ", " )}" );

				OnMessage ( message );

				if ( CommandHandlers.TryGetValue ( command, out var tuple ) )
				{
					await tuple.handler ( message ).ConfigureAwait ( false );
					return;
				}

				if ( MenuManager.TryGetMenu ( message.From, message.Chat.Id, out var menu ) )
					await menu.HandleMessageAsync ( message ).ConfigureAwait ( false );
			}
			catch ( Exception exception )
			{
				Logger.Error ( exception );
			}
		}

		protected virtual void OnMessage ( [UsedImplicitly] Message message )
		{
		}

		protected abstract Task OnStartAsync ( );
	}
}