using System.Collections.Generic;

namespace CryptoTickerBot.Extensions
{
	public static class ListExtensions
	{
		public static string Join<T> ( this IEnumerable<T> enumerable, string delimiter ) =>
			string.Join ( delimiter, enumerable );
	}
}