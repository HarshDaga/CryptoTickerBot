using System.Collections.Generic;

namespace CryptoTickerBot.Extensions
{
	public static class ListExtensions
	{
		public static string Join ( this IEnumerable<string> enumerable, string delimiter ) =>
			string.Join ( delimiter, enumerable );
	}
}