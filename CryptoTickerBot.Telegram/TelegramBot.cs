using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Extensions;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Domain;
using CryptoTickerBot.Telegram.Extensions;
using CryptoTickerBot.Telegram.Menus;
using CryptoTickerBot.Telegram.Menus.Abstractions;
using CryptoTickerBot.Telegram.Subscriptions;
using EnumsNET;
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
		public TelegramBotData Data { get; }
		public BotConfig Config { get; set; }
		public Policy Policy { get; set; }
		public Policy RetryForeverPolicy { get; set; }

		public CancellationToken CancellationToken => Ctb.Cts.Token;

		private readonly Dictionary<string, (string usage, CommandHandlerDelegate handler)> commandHandlers;

		private readonly ConcurrentDictionary<int, ITelegramKeyboardMenu> menuStates =
			new ConcurrentDictionary<int, ITelegramKeyboardMenu> ( );

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

			Data       =  new TelegramBotData ( );
			Data.Error += exception => Logger.Error ( exception );

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

			commandHandlers = new Dictionary<string, (string, CommandHandlerDelegate)>
			{
				["/menu"] = ( "/menu", HandleMenuCommand ),
				["/subscribe"] = ( "/subscribe [Exchange] [Percentage] [Silent=true/false] [Symbols]",
				                   HandleSubscribeCommand )
			};
		}

		public async Task StartAsync ( )
		{
			Logger.Info ( "Starting Telegram Bot" );
			try
			{
				await Policy
					.ExecuteAsync ( async ( ) =>
					{
						Self = await Client.GetMeAsync ( CancellationToken );
						Logger.Info ( $"Hello! My name is {Self.FirstName}" );

						Client.StartReceiving ( cancellationToken: CancellationToken );
					} )
					.ConfigureAwait ( false );

				await ResumeSubscriptions ( );
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

		#region Helpers

		private async Task ResumeSubscriptions ( )
		{
			foreach ( var subscription in Data.PercentChangeSubscriptions )
			{
				subscription.Trigger += async ( sub,
				                                old,
				                                current ) =>
					Data.AddOrUpdate ( sub as TelegramPercentChangeSubscription );
				await subscription.Resume ( this );
			}
		}

		private void UpdateMenuState ( int id,
		                               ITelegramKeyboardMenu menu )
		{
			menuStates.TryRemove ( id, out _ );
			if ( menu != null )
				menuStates[menu.Id] = menu;
		}

		private async Task<bool> MenuTextInput ( Message message )
		{
			foreach ( var menu in GetOpenMenus ( message.From, message.Chat ) )
				await menu.HandleMessageAsync ( message ).ConfigureAwait ( false );

			return true;
		}

		private async Task CloseExistingMenus ( User user )
		{
			foreach ( var menu in menuStates.Values.Where ( x => x != null && x.User == user ).ToList ( ) )
			{
				await menu.DeleteMenu ( ).ConfigureAwait ( false );
				menuStates.TryRemove ( menu.Id, out _ );
			}
		}

		private IEnumerable<ITelegramKeyboardMenu> GetOpenMenus ( User user,
		                                                          Chat chat )
		{
			foreach ( var menu in menuStates.Values
				.Where ( x => x != null && x.User == user && x.Chat.Id == chat.Id )
				.ToList ( ) )
				yield return menu;
		}

		public async Task AddOrUpdateSubscription ( TelegramPercentChangeSubscription subscription )
		{
			var list = Data.GetPercentChangeSubscriptions ( x => x.IsSimilarTo ( subscription ) );

			var existing = list.SingleOrDefault ( x => x.Threshold == subscription.Threshold );
			if ( existing is null )
			{
				Data.AddOrUpdate ( subscription );
				subscription.Trigger += async ( sub,
				                                old,
				                                current ) =>
					Data.AddOrUpdate ( sub as TelegramPercentChangeSubscription );
				await subscription.Start ( this, true );

				return;
			}

			await existing.MergeWith ( subscription );
			Data.AddOrUpdate ( existing );
		}

		#endregion

		#region Command Handlers

		private async Task HandleMenuCommand ( Message message )
		{
			var from = message.From;

			await CloseExistingMenus ( message.From );

			var menu = new MainMenu ( this, from, message.Chat );
			await menu.Display ( ).ConfigureAwait ( false );
			menuStates[menu.Id] = menu;
		}

		private async Task HandleSubscribeCommand ( Message message )
		{
			var from = message.From;
			message.ExtractCommand ( Self, out var command, out var parameters );

			if ( parameters.Count < 3 )
			{
				await Client.SendTextBlockAsync ( message.Chat,
				                                  $"Usage:\n{commandHandlers[command].usage}",
				                                  cancellationToken: CancellationToken );
				return;
			}

			if ( !Enums.TryParse ( parameters[0], true, out CryptoExchangeId exchangeId ) )
			{
				await Client.SendTextBlockAsync ( message.Chat,
				                                  $"{parameters[0]} is not a valid Exchange name",
				                                  cancellationToken: CancellationToken );
				return;
			}

			if ( !decimal.TryParse ( parameters[1].Trim ( '%' ), out var threshold ) )
			{
				await Client.SendTextBlockAsync ( message.Chat,
				                                  $"{parameters[1]} is not a valid percentage value",
				                                  cancellationToken: CancellationToken );
				return;
			}

			var index = bool.TryParse ( parameters[2].ToLower ( ), out var isSilent ) ? 3 : 2;

			var subscription = new TelegramPercentChangeSubscription (
				message.Chat,
				from,
				exchangeId,
				threshold / 100m,
				isSilent,
				parameters
					.Skip ( index )
					.Select ( x => x.Trim ( ' ', ',' ) )
			);

			await AddOrUpdateSubscription ( subscription );
		}

		#endregion

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
			if ( !Data.Users.Contains ( from ) )
				Data.AddOrUpdate ( from, UserRole.Guest );

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
						0,
						cancellationToken: CancellationToken
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

				message.ExtractCommand ( Self, out var command, out var parameters );
				Logger.Debug ( $"Received from {message.From} : {command} {parameters.Join ( ", " )}" );

				if ( commandHandlers.TryGetValue ( command, out var tuple ) )
				{
					await tuple.handler ( message ).ConfigureAwait ( false );
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