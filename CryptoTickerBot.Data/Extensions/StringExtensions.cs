using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CryptoTickerBot.Data.Extensions
{
	public static class StringExtensions
	{
		public static int CaseInsensitiveHashCode ( this string str ) =>
			StringComparer.OrdinalIgnoreCase.GetHashCode ( str );

		public static T ToObject<T> ( this string json ) =>
			JsonConvert.DeserializeObject<T> ( json );

		public static T ToObject<T> ( this string json,
		                              params JsonConverter[] converters ) =>
			JsonConvert.DeserializeObject<T> ( json, converters );

		public static T ToObject<T> ( this string json,
		                              JsonSerializerSettings settings ) =>
			JsonConvert.DeserializeObject<T> ( json, settings );

		public static IEnumerable<string> SplitOnLength ( this string input,
		                                                  int length )
		{
			var index = 0;
			while ( index < input.Length )
			{
				if ( index + length < input.Length )
					yield return input.Substring ( index, length );
				else
					yield return input.Substring ( index );

				index += length;
			}
		}

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