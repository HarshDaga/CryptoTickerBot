using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTickerBot.Exchanges;
using CryptoTickerBot.Extensions;
using CryptoTickerBot.Helpers;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using TelegramBot.Extensions;
using CTB = CryptoTickerBot.Core.Bot;

namespace TelegramBot
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
					MessageText = $"```\n{name}\n{exchange.ToString ( fiat )}\n```",
					ParseMode = ParseMode.Markdown
				}
			};

		private static void StartCryptoTickerBot ( )
		{
			exchanges = CTB.Exchanges;
			CTB.Start ( );
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

				case "/compare":
					await HandleCompare ( message );
					break;
			}
		}

		private static async Task HandleCompare ( Message message )
		{
			var compare = CTB.CompareTable.GetAll ( );

			CryptoCompareTable.RemoveExchange ( compare, CryptoExchange.Binance );

			var table = new StringBuilder ( );
			foreach ( var from in compare )
			{
				table.AppendLine ( $"{from.Key}" );
				table.AppendLine ( $"{"Symbol",-8}{from.Value.Keys.Select ( x => $"{x,-10}" ).Join ( "" )}" );

				var symbols = ExtractSymbols ( from );

				foreach ( var symbol in symbols )
				{
					table.Append ( $"{symbol,-8}" );
					foreach ( var value in from.Value )
						table.Append (
							value.Value.ContainsKey ( symbol )
								? $"{value.Value[symbol],-10:P}"
								: $"{"",-10}"
						);
					table.AppendLine ( );
				}

				table.AppendLine ( );
			}

			Logger.Info ( $"Sending compare data to {message.From.Username}" );
			await bot.ReplyTextMessageAsync (
				message,
				$"```\n{table}```",
				ParseMode.Markdown );
		}

		private static IList<string> ExtractSymbols (
			KeyValuePair<CryptoExchange, Dictionary<CryptoExchange, Dictionary<string, decimal>>> from
		) =>
			from.Value.Values.Aggregate (
				new List<string> ( ),
				( current, to ) =>
					current.Union ( to.Keys )
						.OrderBy ( x => x )
						.ToList ( )
			);

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