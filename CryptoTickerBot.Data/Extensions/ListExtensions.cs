using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace CryptoTickerBot.Data.Extensions
{
	public static class ListExtensions
	{
		[DebuggerStepThrough]
		[Pure]
		public static string Join<T> ( this IEnumerable<T> enumerable,
		                               string delimiter ) =>
			string.Join ( delimiter, enumerable );

		[DebuggerStepThrough]
		[Pure]
		public static T GetFieldValue<T> ( this object obj,
		                                   string name )
		{
			var field = obj.GetType ( )
				.GetField ( name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
			return (T) field?.GetValue ( obj );
		}

		[DebuggerStepThrough]
		[Pure]
		public static object GetFieldValue ( this object obj,
		                                     string name )
		{
			var field = obj.GetType ( )
				.GetField ( name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
			return field?.GetValue ( obj );
		}
	}
}