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

		private readonly Bot ctb = new Bot ( );
		private readonly TeleBotUserList users;
		private TelegramBotClient bot;
		private Dictionary<CryptoExchange, CryptoExchangeBase> exchanges;
		private User me;

		public string BotToken { get; }

		public TeleBot ( string botToken )
		{
			BotToken      = botToken;
			subscriptions = new List<CryptoExchangeObserver.ResumableSubscription> ( );
			users         = new TeleBotUserList ( Settings.Instance.UsersFileName );

			commands = new
				Dictionary<string, (UserRole role, MessageHandlerDelegate func)>
				{
					["/fetch"]       = (UserRole.Registered, HandleFetch),
					["/compare"]     = (UserRole.Registered, HandleCompare),
					["/best"]        = (UserRole.Registered, HandleBest),
					["/status"]      = (UserRole.Registered, HandleStatus),
					["/subscribe"]   = (UserRole.Registered, HandleSubscribe),
					["/unsubscribe"] = (UserRole.Registered, HandleUnsubscribe),
					["/whitelist"]   = (UserRole.Admin, HandleWhitelist),
					["/restart"]     = (UserRole.Admin, HandleRestart)
				};
		}

		public void Start ( )
		{
			try
			{
				StartCryptoTickerBot ( );

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
			Logger.Debug ( $"Received inline query from: {eventArgs.InlineQuery.From.Username}" );

			var inlineQueryResults = exchanges.Values
				.Select ( x => ToInlineQueryResult ( x, x.Name ) )
				.ToList<InlineQueryResult> ( );

			inlineQueryResults.Add ( ToInlineQueryResult ( exchanges[CryptoExchange.Koinex], "Koinex INR", FiatCurrency.INR ) );
			inlineQueryResults.Add ( ToInlineQueryResult ( exchanges[CryptoExchange.CoinDelta], "CoinDelta INR",
				FiatCurrency.INR ) );
			inlineQueryResults.Add ( ToInlineQueryResult ( exchanges[CryptoExchange.BitBay], "BitBay PLN", FiatCurrency.PLN ) );

			await bot.AnswerInlineQueryAsync (
				eventArgs.InlineQuery.Id,
				inlineQueryResults.ToArray ( ),
				0
			);
		}

		private static InlineQueryResultArticle ToInlineQueryResult (
			CryptoExchangeBase exchange,
			string name,
			FiatCurrency fiat = FiatCurrency.USD
		) =>
			new InlineQueryResultArticle
			{
				Id                  = name,
				HideUrl             = true,
				Title               = name,
				Url                 = exchange.Url,
				InputMessageContent = new InputTextMessageContent
				{
					MessageText = $"```\n{name}\n{exchange.ToTable ( fiat )}\n```",
					ParseMode   = ParseMode.Markdown
				}
			};

		private void StartCryptoTickerBot ( )
		{
			exchanges = ctb.Exchanges;
			ctb.Start ( );
		}

		private async void BotClientOnMessage ( object sender, MessageEventArgs messageEventArgs )
		{
			try
			{
				var message = messageEventArgs.Message;

				if ( message == null || message.Type != MessageType.TextMessage )
					return;

				var text = message.Text;
				var command = text.Split ( ' ' ).First ( );
				if ( command.Contains ( $"@{me.Username}" ) )
					command = command.Substring ( 0, command.IndexOf ( $"@{me.Username}", StringComparison.Ordinal ) );
				var userName = messageEventArgs.Message.From.Username;
				Logger.Debug ( $"Message received from {userName}: {message.Text}" );

				if ( !users.Contains ( userName ) )
				{
					Logger.Info ( $"First message received from {userName}" );
					users.Add ( new TeleBotUser ( userName ) );
				}

				if ( !commands.Keys.Contains ( command ) ) return;

				if ( Settings.Instance.WhitelistMode && !users.HasFlag ( userName, UserRole.Registered ) )
				{
					await RequestPurchase ( message, userName );
					return;
				}

				if ( !users.HasFlag ( userName, commands[command].role ) )
				{
					await SendBlockText ( message, $"You do not have access to {command}" );
					return;
				}

				var parameters = text.Split ( ' ' ).Skip ( 1 ).ToList ( );

				await commands[command].func ( message, parameters );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		private bool IsWhitelisted ( string userName ) =>
			!Settings.Instance.WhitelistMode || users.HasFlag ( userName, UserRole.Registered );

		private delegate Task MessageHandlerDelegate ( Message message, IList<string> @params );
	}
}