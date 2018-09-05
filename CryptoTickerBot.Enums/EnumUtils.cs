using System;
using System.Diagnostics.Contracts;
using static EnumsNET.Enums;

namespace CryptoTickerBot.Enums
{
	public static class EnumUtils
	{
		#region String to Enum

		[Pure]
		public static T ParseEnum<T> (
			string str,
			bool ignoreCase = true,
			bool throwException = true
		)
			where T : struct, Enum =>
			ParseEnum ( str, default ( T ), ignoreCase, throwException );

		[Pure]
		public static T ParseEnum<T> (
			string str,
			T defaultValue,
			bool ignoreCase = true,
			bool throwException = false
		)
			where T : struct, Enum
		{
			var success = TryParse ( str, ignoreCase, out T result );
			if ( !success && throwException ) throw new InvalidOperationException ( "Invalid Cast" );

			return success ? result : defaultValue;
		}

		#endregion

		#region Int to Enum

		[Pure]
		public static T ParseEnum<T> ( int input,
		                               bool throwException = true )
			where T : struct, Enum =>
			ParseEnum ( input, default ( T ), throwException );

		[Pure]
		public static T ParseEnum<T> ( int input,
		                               T defaultValue,
		                               bool throwException = false )
			where T : struct, Enum
		{
			if ( TryToObject ( input, out T returnEnum ) )
				return returnEnum;

			if ( throwException ) throw new InvalidOperationException ( "Invalid Cast" );

			return defaultValue;
		}

		#endregion

		#region String Extension Methods for Enum Parsing

		[Pure]
		public static T ToEnum<T> ( this string str,
		                            bool ignoreCase = true,
		                            bool throwException = true )
			where T : struct, Enum =>
			ParseEnum<T> ( str, ignoreCase, throwException );

		[Pure]
		public static T ToEnum<T> ( this string str,
		                            T defaultValue,
		                            bool ignoreCase = true,
		                            bool throwException = false )
			where T : struct, Enum =>
			ParseEnum ( str, defaultValue, ignoreCase, throwException );

		#endregion

		#region Int Extension Methods for Enum Parsing

		[Pure]
		public static T ToEnum<T> ( this int input,
		                            bool throwException = true )
			where T : struct, Enum =>
			ParseEnum ( input, default ( T ), throwException );

		[Pure]
		public static T ToEnum<T> ( this int input,
		                            T defaultValue,
		                            bool throwException = false )
			where T : struct, Enum =>
			ParseEnum ( input, defaultValue, throwException );

		#endregion
	}
}