using System.Collections.Generic;
using CryptoTickerBot.Exchanges;
using Tababular;

namespace CryptoTickerBot.Extensions
{
	public static class ListExtensions
	{
		public static string Join<T> ( this IEnumerable<T> enumerable, string delimiter ) =>
			string.Join ( delimiter, enumerable );

		public static string ToTable ( this IEnumerable<CryptoExchangeBase> exchanges )
		{
			var formatter = new TableFormatter ( );
			var objects = new List<object> ( );
			var tables = new List<string> ( );

			foreach ( var exchange in exchanges )
			{
				tables.Add ( exchange.Name );
				objects.Clear ( );
				foreach ( var coin in exchange.ExchangeData.Values )
				{
					objects.Add ( new
					{
						coin.Symbol,
						Bid = $"{coin.HighestBid:C}",
						Ask = $"{coin.LowestAsk:C}",
						Spread = $"{coin.SpreadPercentange:P}"
					} );
				}
				tables.Add ( formatter.FormatObjects ( objects ) );
			}
			return tables.Join ( "\n" );
		}
	}
}