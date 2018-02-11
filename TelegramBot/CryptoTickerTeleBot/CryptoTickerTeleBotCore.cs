using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core;
using CryptoTickerBot.Exchanges;
using CryptoTickerBot.Helpers;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;

namespace TelegramBot.CryptoTickerTeleBot
{
	public partial class TeleBot
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		private readonly Dictionary<string, (UserRole role, MessageHandlerDelegate func)> commands;

		private readonly Bot ctb;
		private readonly Dictionary<CryptoExchange, CryptoExchangeBase> exchanges;
		private readonly TeleBotUserList users;
		private TelegramBotClient bot;
		private User me;

		public string BotToken { get; }

		public TeleBot ( string botToken, Bot ctb )
		{
			BotToken      = botToken;
			subscriptions = new List<CryptoExchangeObserver.ResumableSubscription> ( );
			users         = new TeleBotUserList ( Settings.Instance.UsersFileName );
			this.ctb      = ctb;
			exchanges     = ctb.Exchanges;

			commands = new
				Dictionary<string, (UserRole role, MessageHandlerDelegate func)>
				{
					["/status"]      = ( UserRole.Guest, HandleStatus ),
					["/fetch"]       = ( UserRole.Guest, HandleFetch ),
					["/compare"]     = ( UserRole.Registered, HandleCompare ),
					["/best"]        = ( UserRole.Registered, HandleBest ),
					["/subscribe"]   = ( UserRole.Registered, HandleSubscribe ),
					["/unsubscribe"] = ( UserRole.Registered, HandleUnsubscribe ),
					["/register"]    = ( UserRole.Admin, HandleRegister ),
					["/restart"]     = ( UserRole.Admin, HandleRestart ),
					["/users"]       = ( UserRole.Admin, HandleUsers ),
					["/kill"]        = ( UserRole.Admin, HandleKill )
				};
		}

		public void Start ( )
		{
			try
			{
				bot                =  new TelegramBotClient ( BotToken );
				bot.OnMessage      += BotClientOnMessage;
				bot.OnInlineQuery  += BotClientOnInlineQuery;
				bot.OnReceiveError += BotClientOnReceiveError;

				me = bot.GetMeAsync ( ).Result;
				Logger.Info ( $"Hello! My name is {me.FirstName}" );

				bot.StartReceiving ( );

				while ( !ctb.IsInitialized )
					Thread.Sleep ( 10 );

				LoadSubscriptions ( );
				ResumeSubscriptions ( );

				Console.ReadLine ( );
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
			var userName = eventArgs.InlineQuery.From.Username;
			Logger.Debug ( $"Received inline query from: {userName}" );
			if ( !users.Contains ( userName ) )
				users.Add ( new TeleBotUser ( userName ) );

			var fiat = eventArgs.InlineQuery.Query.ToFiatCurrency ( );
			var inlineQueryResults = exchanges.Values
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

				ParseMessage ( message, out var command, out var parameters, out var userName );
				Logger.Debug ( $"Message received from {userName}: {message.Text}" );

				if ( await ValidateUserCommand ( userName, command, message ) )
					return;

				await commands[command].func ( message, parameters );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		private delegate Task MessageHandlerDelegate ( Message message, IList<string> @params );
	}
}