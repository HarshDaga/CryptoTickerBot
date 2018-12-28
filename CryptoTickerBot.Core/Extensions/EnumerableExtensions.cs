using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using CryptoTickerBot.Core.Interfaces;

namespace CryptoTickerBot.Core.Extensions
{
	public static class EnumerableExtensions
	{
		[Pure]
		public static decimal Product ( this IEnumerable<decimal> enumerable ) =>
			enumerable.Aggregate ( 1m, ( cur,
			                             next ) => cur * next );

		[Pure]
		public static double Product ( this IEnumerable<double> enumerable ) =>
			enumerable.Aggregate ( 1d, ( cur,
			                             next ) => cur * next );

		[Pure]
		public static int Product ( this IEnumerable<int> enumerable ) =>
			enumerable.Aggregate ( 1, ( cur,
			                            next ) => cur * next );

		[Pure]
		public static IEnumerable<string> ToTables (
			this IEnumerable<ICryptoExchange> exchanges,
			string fiat = "USD"
		) =>
			exchanges.Select ( exchange => $"{exchange.Name}\n{exchange.ToTable ( fiat )}" );
	}
}