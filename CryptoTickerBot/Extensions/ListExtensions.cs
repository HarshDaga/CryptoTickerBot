using System.Collections.Generic;
using CryptoTickerBot.Exchanges;

namespace CryptoTickerBot.Extensions
{
	public static class ListExtensions
	{
		public static string Join<T> ( this IEnumerable<T> enumerable, string delimiter ) =>
			string.Join ( delimiter, enumerable );

		public static string ToTable ( this IEnumerable<CryptoExchangeBase> exchanges )
		{
			var tables = new List<string> ( );

			foreach ( var exchange in exchanges )
			{
				tables.Add ( exchange.Name );
				tables.Add ( exchange.ToString ( ) );
			}
			return tables.Join ( "\n" );
		}
	}
}