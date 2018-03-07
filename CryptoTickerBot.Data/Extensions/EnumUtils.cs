using System;
using System.Diagnostics.Contracts;

namespace CryptoTickerBot.Data.Extensions
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
			where T : struct =>
			ParseEnum ( str, default ( T ), ignoreCase, throwException );

		[Pure]
		public static T ParseEnum<T> (
			string str,
			T defaultValue,
			bool ignoreCase = true,
			bool throwException = false
		)
			where T : struct
		{
			if ( !typeof ( T ).IsEnum || string.IsNullOrEmpty ( str ) )
				throw new InvalidOperationException (
					"Invalid Enum Type or Input String 'inString'. " +
					typeof ( T ) +
					"  must be an Enum"
				);

			var success = Enum.TryParse ( str, ignoreCase, out T result );
			if ( !success && throwException ) throw new InvalidOperationException ( "Invalid Cast" );

			return success ? result : defaultValue;
		}

		#endregion

		#region Int to Enum

		[Pure]
		public static T ParseEnum<T> ( int input, bool throwException = true )
			where T : struct =>
			ParseEnum ( input, default ( T ), throwException );

		[Pure]
		public static T ParseEnum<T> ( int input, T defaultValue, bool throwException = false )
			where T : struct
		{
			var returnEnum = defaultValue;
			if ( !typeof ( T ).IsEnum )
				throw new InvalidOperationException ( "Invalid Enum Type. " + typeof ( T ) + "  must be an Enum" );

			if ( Enum.IsDefined ( typeof ( T ), input ) )
			{
				returnEnum = (T) Enum.ToObject ( typeof ( T ), input );
			}
			else
			{
				if ( throwException ) throw new InvalidOperationException ( "Invalid Cast" );
			}

			return returnEnum;
		}

		#endregion

		#region String Extension Methods for Enum Parsing

		[Pure]
		public static T ToEnum<T> ( this string str, bool ignoreCase = true, bool throwException = true )
			where T : struct =>
			ParseEnum<T> ( str, ignoreCase, throwException );

		[Pure]
		public static T ToEnum<T> ( this string str, T defaultValue, bool ignoreCase = true,
		                            bool throwException = false )
			where T : struct =>
			ParseEnum ( str, defaultValue, ignoreCase, throwException );

		#endregion

		#region Int Extension Methods for Enum Parsing

		[Pure]
		public static T ToEnum<T> ( this int input, bool throwException = true )
			where T : struct =>
			ParseEnum ( input, default ( T ), throwException );

		[Pure]
		public static T ToEnum<T> ( this int input, T defaultValue, bool throwException = false )
			where T : struct =>
			ParseEnum ( input, defaultValue, throwException );

		#endregion
	}
}