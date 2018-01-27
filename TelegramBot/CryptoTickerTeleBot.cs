using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTickerBot.Core;
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

namespace TelegramBot
{
	public class CryptoTickerTeleBot
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private readonly Bot ctb = new Bot ( );
		private TelegramBotClient bot;
		private Dictionary<CryptoExchange, CryptoExchangeBase> exchanges;
		private User me;
		public string BotToken { get; }

		public CryptoTickerTeleBot ( string botToken )
		{
			BotToken = botToken;
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

				Console.ReadLine ( );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
				throw;
			}
		}

		private void BotClientOnReceiveError ( object sender, ReceiveErrorEventArgs receiveErrorEventArgs )
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
					MessageText = $"```\n{name}\n{exchange.ToString ( fiat )}\n```",
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
				Logger.Debug ( $"Message received from {messageEventArgs.Message.From.Username}: {message.Text}" );

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
				}
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		private async Task HandleUnsubscribe ( Message message )
		{
			foreach ( var observer in ctb.Observers.Values )
				observer.Unsubscribe ( message.Chat.Id );

			await bot.ReplyTextMessageAsync (
				message,
				"```\nUnsubscribed from all exchanges.\n```",
				ParseMode.Markdown );
		}

		private async Task HandleSubscribe ( Message message, IList<string> @params )
		{
			var chosen = @params
				.Where ( x => GetExchangeBase ( x ) != null )
				.Select ( x => GetExchangeBase ( x ).Id )
				.ToArray ( );

			if ( chosen.Length == 0 )
			{
				chosen = exchanges.Keys.ToArray ( );
				await bot.ReplyTextMessageAsync (
					message,
					"```\nNo exchanges provided, subscribing to all exchanges.\n```",
					ParseMode.Markdown );
			}

			var threshold = 0.05m;
			var thresholdString = @params.FirstOrDefault ( x => decimal.TryParse ( x.Trim ( '%' ), out var _ ) );
			if ( !string.IsNullOrEmpty ( thresholdString ) )
				threshold = decimal.Parse ( thresholdString ) / 100m;
			else
				await bot.ReplyTextMessageAsync (
					message,
					$"```\nNo threshold provided, setting to default {threshold:P}\n```",
					ParseMode.Markdown );

			foreach ( var exchange in chosen )
				ctb.Observers[exchange].Subscribe ( message.Chat.Id, threshold, async ( ex, oldValue, newValue ) =>
				{
					var change = newValue - oldValue;
					var reply =
						$"{ex.Name,-10}: {newValue.Symbol} {change.Value.ToCurrency ( ),-6} {change.Percentage,6:P} in {change.TimeDiff:dd\\:hh\\:mm\\:ss}";
					await bot.ReplyTextMessageAsync (
						message,
						$"```\n{reply}\n```",
						ParseMode.Markdown );
				} );
		}

		private async Task HandleFetch ( Message message )
		{
			var table = exchanges.Values.ToTable ( );
			Logger.Info ( $"Sending ticker data to {message.From.Username}" );
			await bot.ReplyTextMessageAsync (
				message,
				$"```\n{table}```",
				ParseMode.Markdown );
		}

		private async Task HandleCompare ( Message message, IEnumerable<string> @params )
		{
			var compare = ctb.CompareTable.Get (
				@params
					.Where ( x => GetExchangeBase ( x ) != null )
					.Select ( x => GetExchangeBase ( x ).Id )
					.ToArray ( )
			);

			var table = BuildCompareTable ( compare );

			Logger.Info ( $"Sending compare data to {message.From.Username}" );
			await bot.ReplyTextMessageAsync (
				message,
				$"```\n{table}```",
				ParseMode.Markdown );
		}

		private async Task HandleCompare ( Message message )
		{
			var compare = ctb.CompareTable.GetAll ( );

			var table = BuildCompareTable ( compare );

			Logger.Info ( $"Sending compare data to {message.From.Username}" );
			await bot.ReplyTextMessageAsync (
				message,
				$"```\n{table}```",
				ParseMode.Markdown );
		}

		private StringBuilder BuildCompareTable (
			Dictionary<CryptoExchange, Dictionary<CryptoExchange, Dictionary<string, decimal>>> compare )
		{
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

			return table;
		}

		private async Task HandleBest ( Message message, IList<string> @params )
		{
			if ( @params.Count < 2 )
				return;

			var from = GetExchangeBase ( @params[0] );
			var to = GetExchangeBase ( @params[1] );

			if ( from == null )
			{
				await bot.ReplyTextMessageAsync (
					message,
					$"```\nERROR: {@params[0]} not found.```",
					ParseMode.Markdown );
				return;
			}

			if ( to == null )
			{
				await bot.ReplyTextMessageAsync (
					message,
					$"```\nERROR: {@params[1]} not found.```",
					ParseMode.Markdown );
				return;
			}

			if ( from.Count == 0 || to.Count == 0 )
			{
				await bot.ReplyTextMessageAsync (
					message,
					"```\nERROR: Not enough data received.```",
					ParseMode.Markdown );
				return;
			}

			var (best, leastWorst, profit) = ctb.CompareTable.GetBestPair ( from.Id, to.Id );
			var fees =
				from.ExchangeData[best].Buy ( from.DepositFees[best] ) +
				from.ExchangeData[best].Sell ( from.WithdrawalFees[best] ) +
				to.ExchangeData[leastWorst].Buy ( to.DepositFees[leastWorst] ) +
				to.ExchangeData[leastWorst].Sell ( to.WithdrawalFees[leastWorst] );
			var minInvestment = fees / profit;

			var reply =
				$"Buy  {best} From: {from.Name,-12} @ {from[best].BuyPrice:C}\n" +
				$"Sell {best} To:   {to.Name,-12} @ {to[best].SellPrice:C}\n" +
				$"Buy  {leastWorst} From: {to.Name,-12} @ {to[leastWorst].BuyPrice:C}\n" +
				$"Sell {leastWorst} To:   {from.Name,-12} @ {from[leastWorst].SellPrice:C}\n" +
				$"Expected profit:    {profit:P}\n" +
				$"Estimated fees:     {fees:C}\n" +
				$"Minimum Investment: {minInvestment:C}";

			Logger.Info ( $"Sending best pair data to {message.From.Username}" );
			await bot.ReplyTextMessageAsync (
				message,
				$"```\n{reply}```",
				ParseMode.Markdown );
		}

		private CryptoExchangeBase GetExchangeBase ( string name ) =>
			exchanges.Values
				.AsEnumerable ( )
				.FirstOrDefault ( x => x.Name.Equals ( name, StringComparison.CurrentCultureIgnoreCase ) );

		private async Task HandleBest ( Message message )
		{
			var best = ctb.CompareTable.GetBest ( );

			if ( best.first == null || best.second == null )
			{
				await bot.ReplyTextMessageAsync (
					message,
					"```\nERROR: Not enough data received.```",
					ParseMode.Markdown );
				return;
			}

			var from = exchanges[best.from];
			var to = exchanges[best.to];
			var fees =
				from.ExchangeData[best.first].Buy ( from.DepositFees[best.first] ) +
				from.ExchangeData[best.first].Sell ( from.WithdrawalFees[best.first] ) +
				to.ExchangeData[best.second].Buy ( to.DepositFees[best.second] ) +
				to.ExchangeData[best.second].Sell ( to.WithdrawalFees[best.second] );
			var minInvestment = fees / best.profit;

			var reply =
				$"Buy  {best.first} From: {from.Name,-12} @ {from[best.first].BuyPrice:C}\n" +
				$"Sell {best.first} To:   {to.Name,-12} @ {to[best.first].SellPrice:C}\n" +
				$"Buy  {best.second} From: {to.Name,-12} @ {to[best.second].BuyPrice:C}\n" +
				$"Sell {best.second} To:   {from.Name,-12} @ {from[best.second].SellPrice:C}\n" +
				$"Expected profit:    {best.profit:P}\n" +
				$"Estimated fees:     {fees:C}\n" +
				$"Minimum Investment: {minInvestment:C}";

			Logger.Info ( $"Sending best pair data to {message.From.Username}" );
			await bot.ReplyTextMessageAsync (
				message,
				$"```\n{reply}```",
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
	}
}