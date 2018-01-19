using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Exchanges;
using CryptoTickerBot.Extensions;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram_Bot.Extensions;

namespace Telegram_Bot
{
	public class Program
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private static readonly string BotToken = Settings.Instance.BotToken;
		private static TelegramBotClient bot;
		private static Dictionary<CryptoExchange, CryptoExchangeBase> exchanges;
		private static User me;

		public static void Main ( string[] args )
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

		private static async void BotClientOnInlineQuery ( object sender, InlineQueryEventArgs eventArgs )
		{
			Logger.Debug ( $"Received inline query from: {eventArgs.InlineQuery.From.Username}" );

			var inlineQueryResults = exchanges.Values
				.Select ( exchange => new InlineQueryResultArticle
				{
					Id = exchange.Name,
					HideUrl = true,
					Title = exchange.Name,
					Url = exchange.Url,
					InputMessageContent = new InputTextMessageContent
					{
						MessageText = $"```\n{exchange.Name}\n{exchange.ToString ( )}\n```",
						ParseMode = ParseMode.Markdown
					}
				} )
				.ToList<InlineQueryResult> ( );

			await bot.AnswerInlineQueryAsync (
				eventArgs.InlineQuery.Id,
				inlineQueryResults.ToArray ( )
			);
		}

		private static void StartCryptoTickerBot ( )
		{
			exchanges = CryptoTickerBot.Core.Bot.Exchanges;
			CryptoTickerBot.Core.Bot.Start ( );
		}

		private static async void BotClientOnMessage ( object sender, MessageEventArgs messageEventArgs )
		{
			var message = messageEventArgs.Message;

			if ( message == null || message.Type != MessageType.TextMessage )
				return;

			var text = message.Text;
			var command = text.Split ( ' ' ).First ( );
			if ( command.Contains ( $"@{me.Username}" ) )
				command = command.Substring ( 0, command.IndexOf ( $"@{me.Username}", StringComparison.Ordinal ) );
			Logger.Debug ( $"Message received from {messageEventArgs.Message.From.Username}: {message.Text}" );

			switch ( command )
			{
				case "/fetch":
					await HandleFetch ( message );
					break;
			}
		}

		private static async Task HandleFetch ( Message message )
		{
			var table = exchanges.Values.ToTable ( );
			Logger.Info ( $"Sending ticker data to {message.From.Username}" );
			await bot.ReplyTextMessageAsync (
				message,
				$"```\n{table}```",
				ParseMode.Markdown );
		}
	}
}