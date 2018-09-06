using System;

namespace CryptoTickerBot.Core.Extensions
{
	public static class StringExtensions
	{
		public static string ReplaceLastOccurrence ( this string source,
		                                             string find,
		                                             string replace,
		                                             StringComparison comparison = StringComparison.OrdinalIgnoreCase )
		{
			var place = source.LastIndexOf ( find, comparison );

			return place == -1 ? source : source.Remove ( place, find.Length ).Insert ( place, replace );
		}
	}
}