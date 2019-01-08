using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Telegram.Extensions;
using CryptoTickerBot.Telegram.Menus;
using CryptoTickerBot.Telegram.Menus.Pages;
using CryptoTickerBot.Telegram.Subscriptions;
using EnumsNET;
using Humanizer;
using Humanizer.Localisation;
using NLog;
using Tababular;
using Telegram.Bot.Types;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

#pragma warning disable 1998

namespace CryptoTickerBot.Telegram
{
	public class TelegramBot : TelegramBotBase
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public IBot Ctb { get; }

		public TelegramBotData Data { get; }

		public override CancellationToken CancellationToken => Ctb.Cts.Token;

		public TelegramBot ( TelegramBotConfig config,
		                     IBot ctb ) : base ( config )
		{
			Ctb = ctb;

			Data       =  new TelegramBotData ( );
			Data.Error += exception => Logger.Error ( exception );

			AddCommandHandler ( "/menu", "/menu", HandleMenuCommandAsync );
			AddCommandHandler ( "/subscribe",
			                    "/subscribe [Exchange] [Percentage] [Silent=true/false] [Symbols]",
			                    HandleSubscribeCommandAsync );
			AddCommandHandler ( "/status", "/status", HandleStatusCommandAsync );
			AddCommandHandler ( "/restart", "/restart", HandleRestartCommandAsync );
		}

		protected override async Task OnStartAsync ( )
		{
			await ResumeSubscriptionsAsync ( ).ConfigureAwait ( false );
		}

		protected override async void OnInlineQuery ( InlineQuery query )
		{
			var from = query.From;
			Logger.Info ( $"Received inline query from: {from.Id,-10} {from.FirstName}" );

			if ( !Data.Users.Contains ( from ) )
				Data.AddOrUpdate ( from, UserRole.Guest );

			var words = query.Query.Split ( new[] {' '}, StringSplitOptions.RemoveEmptyEntries );
			var inlineQueryResults = Ctb.Exchanges.Values
				.Select ( x => x.ToInlineQueryResult ( words ) )
				.ToList ( );

			try
			{
				await Client
					.AnswerInlineQueryAsync (
						query.Id,
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
			var user = message.From;
			var chat = message.Chat;

			if ( MenuManager.TryGetMenu ( user, chat.Id, out var menu ) )
			{
				await menu.DeleteAsync ( ).ConfigureAwait ( false );
				MenuManager.Remove ( user, chat.Id );
			}

			menu = new TelegramKeyboardMenu ( user, chat, this );
			MenuManager.AddOrUpdateMenu ( menu );
			await menu.DisplayAsync ( new MainPage ( menu ) ).ConfigureAwait ( false );
		}

		private async Task HandleSubscribeCommandAsync ( Message message )
		{
			var from = message.From;
			var (command, parameters) = message.ExtractCommand ( Self );

			if ( parameters.Count < 3 )
			{
				await Client.SendTextBlockAsync ( message.Chat,
				                                  $"Usage:\n{CommandHandlers[command].usage}",
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
	}
}