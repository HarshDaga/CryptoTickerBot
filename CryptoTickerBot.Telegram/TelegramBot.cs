using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Extensions;
using CryptoTickerBot.Telegram.Extensions;
using CryptoTickerBot.Telegram.Menus;
using CryptoTickerBot.Telegram.Menus.Abstractions;
using CryptoTickerBot.Telegram.Subscriptions;
using EnumsNET;
using Humanizer;
using Humanizer.Localisation;
using NLog;
using Polly;
using Tababular;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

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
		public TelegramBotConfig Config { get; set; }
		public Policy Policy { get; set; }
		public Policy RetryForeverPolicy { get; set; }

		public CancellationToken CancellationToken => Ctb.Cts.Token;

		private readonly Dictionary<string, (string usage, CommandHandlerDelegate handler)> commandHandlers;

		private readonly ConcurrentDictionary<int, ITelegramKeyboardMenu> menuStates =
			new ConcurrentDictionary<int, ITelegramKeyboardMenu> ( );

		public TelegramBot ( TelegramBotConfig config,
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
				["/menu"] = ( "/menu", HandleMenuCommandAsync ),
				["/subscribe"] = ( "/subscribe [Exchange] [Percentage] [Silent=true/false] [Symbols]",
				                   HandleSubscribeCommandAsync ),
				["/status"]  = ( "/status", HandleStatusCommandAsync ),
				["/restart"] = ( "/restart", HandleRestartCommandAsync )
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
						Self = await Client.GetMeAsync ( CancellationToken ).ConfigureAwait ( false );
						Logger.Info ( $"Hello! My name is {Self.FirstName}" );

						Client.StartReceiving ( cancellationToken: CancellationToken );
					} ).ConfigureAwait ( false );

				await ResumeSubscriptionsAsync ( ).ConfigureAwait ( false );
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

		private async Task ResumeSubscriptionsAsync ( )
		{
			foreach ( var subscription in Data.PercentChangeSubscriptions )
			{
				subscription.Trigger += ( sub,
				                          old,
				                          current ) =>
				{
					Data.AddOrUpdate ( sub as TelegramPercentChangeSubscription );
					return Task.CompletedTask;
				};

				await subscription.ResumeAsync ( this ).ConfigureAwait ( false );
			}
		}

		private void UpdateMenuState ( int id,
		                               ITelegramKeyboardMenu menu )
		{
			menuStates.TryRemove ( id, out _ );
			if ( menu != null )
				menuStates[menu.Id] = menu;
		}

		private async Task<bool> MenuTextInputAsync ( Message message )
		{
			foreach ( var menu in GetOpenMenus ( message.From, message.Chat ) )
				await menu.HandleMessageAsync ( message ).ConfigureAwait ( false );

			return true;
		}

		private async Task CloseExistingMenusAsync ( User user )
		{
			foreach ( var menu in menuStates.Values.Where ( x => x != null && x.User == user ).ToList ( ) )
			{
				await menu.DeleteMenuAsync ( ).ConfigureAwait ( false );
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

		public async Task AddOrUpdateSubscriptionAsync ( TelegramPercentChangeSubscription subscription )
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
				await subscription.StartAsync ( this, true ).ConfigureAwait ( false );

				return;
			}

			await existing.MergeWithAsync ( subscription ).ConfigureAwait ( false );
			Data.AddOrUpdate ( existing );
		}

		public string GetStatusString ( )
		{
			var formatter = new TableFormatter ( );
			var objects = Ctb.Exchanges.Values.Select (
					exchange => new Dictionary<string, string>
					{
						["Exchange"]    = exchange.Name,
						["Up Time"]     = exchange.UpTime.Humanize ( 2, minUnit: TimeUnit.Second ),
						["Last Update"] = exchange.LastUpdateDuration.Humanize ( )
					}
				)
				.Cast<IDictionary<string, string>> ( )
				.ToList ( );

			var builder = new StringBuilder ( );
			builder
				.AppendLine (
					$"Running since {( DateTime.UtcNow - Ctb.StartTime ).Humanize ( 3, minUnit: TimeUnit.Second )}" )
				.AppendLine ( "" )
				.AppendLine ( formatter.FormatDictionaries ( objects ) );

			return builder.ToString ( );
		}

		#endregion

		#region Command Handlers

		private async Task HandleMenuCommandAsync ( Message message )
		{
			var from = message.From;

			await CloseExistingMenusAsync ( message.From ).ConfigureAwait ( false );

			var menu = new MainMenu ( this, from, message.Chat );
			await menu.DisplayAsync ( ).ConfigureAwait ( false );
			menuStates[menu.Id] = menu;
		}

		private async Task HandleSubscribeCommandAsync ( Message message )
		{
			var from = message.From;
			var (command, parameters) = message.ExtractCommand ( Self );

			if ( parameters.Count < 3 )
			{
				await Client.SendTextBlockAsync ( message.Chat,
				                                  $"Usage:\n{commandHandlers[command].usage}",
				                                  cancellationToken: CancellationToken ).ConfigureAwait ( false );
				return;
			}

			if ( !Enums.TryParse<CryptoExchangeId> ( parameters[0], true, out var exchangeId ) )
			{
				await Client.SendTextBlockAsync ( message.Chat,
				                                  $"{parameters[0]} is not a valid Exchange name",
				                                  cancellationToken: CancellationToken ).ConfigureAwait ( false );
				return;
			}

			if ( !decimal.TryParse ( parameters[1].Trim ( '%' ), out var threshold ) )
			{
				await Client.SendTextBlockAsync ( message.Chat,
				                                  $"{parameters[1]} is not a valid percentage value",
				                                  cancellationToken: CancellationToken ).ConfigureAwait ( false );
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

			await AddOrUpdateSubscriptionAsync ( subscription ).ConfigureAwait ( false );
		}

		private async Task HandleRestartCommandAsync ( Message message )
		{
			Ctb.RestartExchangeMonitors ( );
			await Client.SendTextBlockAsync ( message.Chat,
			                                  "Restarted exchange monitors",
			                                  cancellationToken: CancellationToken ).ConfigureAwait ( false );
		}

		private async Task HandleStatusCommandAsync ( Message message )
		{
			await Client.SendTextBlockAsync ( message.Chat,
			                                  GetStatusString ( ),
			                                  cancellationToken: CancellationToken ).ConfigureAwait ( false );
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
						                            cancellationToken: Ctb.Cts.Token ).ConfigureAwait ( false );
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

				var (command, parameters) = message.ExtractCommand ( Self );
				Logger.Debug ( $"Received from {message.From} : {command} {parameters.Join ( ", " )}" );

				if ( !Data.Users.Contains ( message.From ) )
					Data.AddOrUpdate ( message.From, UserRole.Guest );

				if ( commandHandlers.TryGetValue ( command, out var tuple ) )
				{
					await tuple.handler ( message ).ConfigureAwait ( false );
					return;
				}

				await MenuTextInputAsync ( message ).ConfigureAwait ( false );
			}
			catch ( Exception exception )
			{
				Logger.Error ( exception );
			}
		}

		#endregion
	}
}