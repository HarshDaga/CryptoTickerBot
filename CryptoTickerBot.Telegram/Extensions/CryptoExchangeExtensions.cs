using System.Text;
using CryptoTickerBot.Core.Interfaces;
using Humanizer;
using Humanizer.Localisation;
using Telegram.Bot.Types.InlineQueryResults;

namespace CryptoTickerBot.Telegram.Extensions
{
	public static class CryptoExchangeExtensions
	{
		public static InlineQueryResultArticle ToInlineQueryResult (
			this ICryptoExchange exchange,
			params string[] symbols
		) =>
			new InlineQueryResultArticle (
				exchange.Name, exchange.Name,
				$"{exchange.Name}\n{exchange.ToTable ( symbols )}".ToMarkdownMessage ( )
			);

		public static string GetSummary ( this ICryptoExchange exchange )
		{
			var sb = new StringBuilder ( );

			sb.AppendLine ( $"Name: {exchange.Name}" );
			sb.AppendLine ( $"Is Running: {exchange.IsStarted}" );
			sb.AppendLine ( $"Url: {exchange.Url}" );
			sb.AppendLine ( $"Up Time: {exchange.UpTime.Humanize ( 2, minUnit: TimeUnit.Second )}" );
			sb.AppendLine ( $"Last Change: {exchange.LastChangeDuration.Humanize ( 2 )}" );
			sb.AppendLine ( $"Base Symbols: {exchange.BaseSymbols.Humanize ( )}" );
			sb.AppendLine ( $"Total Pairs: {exchange.Count}" );

			return sb.ToString ( );
		}
	}
}