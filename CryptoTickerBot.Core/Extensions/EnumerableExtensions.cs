using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using CryptoTickerBot.Core.Interfaces;

namespace CryptoTickerBot.Core.Extensions
{
	public static class EnumerableExtensions
	{
		[Pure]
		public static IEnumerable<string> ToTables (
			this IEnumerable<ICryptoExchange> exchanges,
			string fiat = "USD"
		) =>
			exchanges.Select ( exchange => $"{exchange.Name}\n{exchange.ToTable ( fiat )}" );
	}
}