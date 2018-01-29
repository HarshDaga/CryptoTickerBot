using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using File = System.IO.File;

namespace TelegramBot.CryptoTickerTeleBot
{
	public partial class TeleBot
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private static readonly object WhitelistLock = new object ( );

		private static readonly string[] Commands =
		{
			"/fetch",
			"/compare",
			"/best",
			"/status",
			"/subscribe",
			"/unsubscribe",
			"/whitelist",
			"/restart"
		};

		private readonly Bot ctb = new Bot ( );
		private TelegramBotClient bot;
		private Dictionary<CryptoExchange, CryptoExchangeBase> exchanges;
		private User me;

		public string BotToken { get; }

		public TeleBot ( string botToken )
		{
			BotToken = botToken;
			subscriptions = new List<CryptoExchangeObserver.ResumableSubscription> ( );
		}

		public void Start ( )
		{
			try
			{
				StartCryptoTickerBot ( );

				bot = new TelegramBotClient ( BotToken );
				bot.OnMessage += BotClientOnMessage;
				bot.OnInlineQuery += BotClientOnInlineQuery;
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
				Id = name,
				HideUrl = true,
				Title = name,
				Url = exchange.Url,
				InputMessageContent = new InputTextMessageContent
				{
					MessageText = $"```\n{name}\n{exchange.ToTable ( fiat )}\n```",
					ParseMode = ParseMode.Markdown
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

				if ( Commands.Contains ( command ) && !IsWhitelisted ( userName ) )
				{
					await RequestPurchase ( message, userName );
					return;
				}

				switch ( command )
				{
					case "/fetch":
						await HandleFetch ( message );
						break;

					case "/compare":
						if ( text.Count ( x => x == ' ' ) < 2 )
							await HandleCompare ( message );
						else
							await HandleCompare ( message, text.Split ( ' ' ).Skip ( 1 ).ToList ( ) );
						break;

					case "/best":
						if ( text.Count ( x => x == ' ' ) < 2 )
							await HandleBest ( message );
						else
							await HandleBest ( message, text.Split ( ' ' ).Skip ( 1 ).ToList ( ) );
						break;

					case "/subscribe":
						await HandleSubscribe ( message, text.Split ( ' ' ).Skip ( 1 ).ToList ( ) );
						break;

					case "/unsubscribe":
						await HandleUnsubscribe ( message );
						break;

					case "/status":
						await HandleStatus ( message );
						break;

					case "/whitelist":
						if ( Settings.Instance.Admins?.Contains ( userName ) == true )
							await HandleWhitelist ( message, text.Split ( ' ' ).Skip ( 1 ).FirstOrDefault ( ) );
						break;

					case "/restart":
						await HandleRestart ( message );
						break;
				}
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		private static bool IsWhitelisted ( string userName )
		{
			if ( !Settings.Instance.WhitelistMode || Settings.Instance.Admins.Contains ( userName ) )
				return true;

			lock ( WhitelistLock )
			{
				if ( !File.Exists ( Settings.Instance.WhiteListFileName ) )
					File.Create ( Settings.Instance.WhiteListFileName );

				var whitelist = File.ReadAllLines ( Settings.Instance.WhiteListFileName );
				return whitelist.Contains ( userName );
			}
		}
	}
}