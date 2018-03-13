using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Persistence;
using CryptoTickerBot.Exchanges.Core;
using CryptoTickerBot.Helpers;
using JetBrains.Annotations;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using TelegramBot.Extensions;
using CTB = CryptoTickerBot.Core.CryptoTickerBot;

namespace TelegramBot.Core
{
	public partial class TeleBot
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private static readonly Logger TeleMessageLogger = LogManager.GetLogger ( "TeleMessageReceived" );

		private TelegramBotClient bot;

		private Dictionary<string, (UserRole role, MessageHandlerDelegate handler)> commands;
		private User me;

		public CTB Ctb { get; private set; }

		public Dictionary<CryptoExchangeId, CryptoExchangeBase> Exchanges => Ctb.Exchanges;

		public string BotToken { get; }
		public List<TelegramBotUser> Users { get; private set; }

		public TeleBot ( string botToken, CTB ctb )
		{
			BotToken      = botToken;
			Subscriptions = new List<TelegramSubscription> ( );
			Ctb           = ctb;

			FetchUserList ( );

			InitializeCommands ( );
		}

		[UsedImplicitly]
		public event Action<TeleBot> Restart;

		private void InitializeCommands ( )
		{
			commands = new
				Dictionary<string, (UserRole role, MessageHandlerDelegate handler)>
				{
					["/status"]      = ( UserRole.Guest, HandleStatus ),
					["/fetch"]       = ( UserRole.Guest, HandleFetch ),
					["/compare"]     = ( UserRole.Registered, HandleCompare ),
					["/best"]        = ( UserRole.Registered, HandleBest ),
					["/subscribe"]   = ( UserRole.Registered, HandleSubscribe ),
					["/unsubscribe"] = ( UserRole.Registered, HandleUnsubscribe ),
					["/restart"]     = ( UserRole.Admin, HandleRestart ),
					["/users"]       = ( UserRole.Admin, HandleUsers ),
					["/kill"]        = ( UserRole.Admin, HandleKill ),
					["/putgroup"]    = ( UserRole.Owner, HandlePutGroup )
				};
		}

		private void FetchUserList ( ) =>
			UnitOfWork.Do ( u => Users = u.Users.GetAll ( )
				                .Select ( x => (TelegramBotUser) x )
				                .ToList ( )
			);

		public void Start ( )
		{
			Logger.Info ( "Starting Telegram Bot" );
			try
			{
				bot                =  new TelegramBotClient ( BotToken );
				bot.OnMessage      += BotClientOnMessage;
				bot.OnInlineQuery  += BotClientOnInlineQuery;
				bot.OnReceiveError += BotClientOnReceiveError;

				me = bot.GetMeAsync ( ).Result;
				Logger.Info ( $"Hello! My name is {me.FirstName}" );

				bot.StartReceiving ( );

				while ( !Ctb.IsInitialized )
					Thread.Sleep ( 10 );

				LoadSubscriptions ( );
				SendResumeNotifications ( );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
				throw;
			}
		}

		private static void BotClientOnReceiveError ( object sender, ReceiveErrorEventArgs receiveErrorEventArgs )
		{
			Logger.Error (
				receiveErrorEventArgs.ApiRequestException,
				$"Error Code: {receiveErrorEventArgs.ApiRequestException.ErrorCode}"
			);
		}

		private async void BotClientOnInlineQuery ( object sender, InlineQueryEventArgs eventArgs )
		{
			var from = eventArgs.InlineQuery.From;
			Logger.Info ( $"Received inline query from: {from.Id,-10} {from.FirstName}" );
			if ( !Users.Contains ( from.Id ) )
			{
				var user = new TelegramBotUser ( from );
				Users.Add ( user );
				UnitOfWork.Do ( u => u.Users.AddOrUpdate ( user ) );
			}

			var fiat = eventArgs.InlineQuery.Query.ToFiatCurrency ( );
			var inlineQueryResults = Exchanges.Values
				.Select ( x => ToInlineQueryResult ( x, x.Name, fiat ) )
				.ToList<InlineQueryResult> ( );

			try
			{
				await bot.AnswerInlineQueryAsync (
					eventArgs.InlineQuery.Id,
					inlineQueryResults.ToArray ( ),
					0
				);
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		private static InlineQueryResultArticle ToInlineQueryResult (
			CryptoExchangeBase exchange,
			string name,
			FiatCurrency fiat = FiatCurrency.USD
		) =>
			new InlineQueryResultArticle
			{
				Id      = name,
				HideUrl = true,
				Title   = name,
				Url     = exchange.Url,
				InputMessageContent = new InputTextMessageContent
				{
					MessageText = $"```\n{name}\n{exchange.ToTable ( fiat )}\n```",
					ParseMode   = ParseMode.Markdown
				}
			};

		private async void BotClientOnMessage ( object sender, MessageEventArgs messageEventArgs )
		{
			try
			{
				var message = messageEventArgs.Message;

				if ( message == null || message.Type != MessageType.TextMessage )
					return;

				var from = message.From;
				ParseMessage ( message, out var command, out var parameters );
				TeleMessageLogger.Info ( $"{from.Id,-10} {from.FirstName}: {message.Text}" );

				if ( await ValidateUserCommand ( from, command, message ) )
					return;

				await commands[command].handler ( message, parameters );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		private delegate Task MessageHandlerDelegate ( Message message, IList<string> @params );
	}
}