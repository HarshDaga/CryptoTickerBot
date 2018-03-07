using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Exchanges.Core;

namespace CryptoTickerBot.Extensions
{
	public static class ListExtensions
	{
		[DebuggerStepThrough]
		[Pure]
		public static string Join<T> ( this IEnumerable<T> enumerable, string delimiter ) =>
			string.Join ( delimiter, enumerable );

		[DebuggerStepThrough]
		[Pure]
		public static IList<T> TakeLast<T> ( this IList<T> source, int count ) =>
			source.Skip ( Math.Max ( 0, source.Count - count ) ).ToList ( );

		[DebuggerStepThrough]
		[Pure]
		public static T[] TakeLast<T> ( this T[] source, int count ) =>
			source.Skip ( Math.Max ( 0, source.Length - count ) ).ToArray ( );

		[Pure]
		public static IEnumerable<string> ToTables (
			this IEnumerable<CryptoExchangeBase> exchanges,
			FiatCurrency fiat = FiatCurrency.USD
		) =>
			exchanges.Select ( exchange => $"{exchange.Name}\n{exchange.ToTable ( fiat )}" );

		[DebuggerStepThrough]
		[Pure]
		public static T GetFieldValue<T> ( this object obj, string name )
		{
			var field = obj.GetType ( ).GetField ( name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
			return (T) field?.GetValue ( obj );
		}

		[DebuggerStepThrough]
		[Pure]
		public static object GetFieldValue ( this object obj, string name )
		{
			var field = obj.GetType ( ).GetField ( name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
			return field?.GetValue ( obj );
		}

		public static IList<TimeInterval<T>> ToTimeIntervals<T> ( this ReplaySubject<T> source )
		{
			var result = source.GetFieldValue ( "_implementation" );
			var queue = result.GetFieldValue<Queue<TimeInterval<T>>> ( "_queue" );
			return queue.ToList ( );
		}

		public static IObservable<IEnumerable<T>> SlidingWindow<T> ( this IObservable<T> o, int length )
		{
			var window = new Queue<T> ( );

			return o.Scan<T, IEnumerable<T>> ( new T[0], ( a, b ) =>
			{
				window.Enqueue ( b );
				if ( window.Count > length )
					window.Dequeue ( );
				return window.ToArray ( );
			} );
		}
	}
}