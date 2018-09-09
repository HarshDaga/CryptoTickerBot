using CryptoTickerBot.Core.Interfaces;
using Telegram.Bot.Types.InlineQueryResults;

namespace CryptoTickerBot.Telegram.Extensions
{
	// TODO split into extension classes
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
	}
}