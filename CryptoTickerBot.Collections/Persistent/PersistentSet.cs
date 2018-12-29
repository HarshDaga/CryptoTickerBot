using System;
using System.Collections.Generic;
using CryptoTickerBot.Collections.Persistent.Base;

namespace CryptoTickerBot.Collections.Persistent
{
	public sealed class PersistentSet<T> : PersistentCollection<T, HashSet<T>>, ISet<T>
	{
		private PersistentSet ( string fileName,
		                        TimeSpan flushInterval )
			: base ( fileName, DefaultSerializerSettings, flushInterval )
		{
		}

		public static PersistentSet<T> Build ( string fileName ) =>
			Build ( fileName, DefaultFlushInterval );

		public static PersistentSet<T> Build ( string fileName,
		                                       TimeSpan flushInterval )
		{
			var collection = GetOpenCollection<PersistentSet<T>> ( fileName );

			return collection ?? new PersistentSet<T> ( fileName, flushInterval );
		}

		bool ISet<T>.Add ( T item )
		{
			var result = Collection.Add ( item );
			Save ( );

			return result;
		}

		public void ExceptWith ( IEnumerable<T> other )
		{
			Collection.ExceptWith ( other );
		}

		public void IntersectWith ( IEnumerable<T> other )
		{
			Collection.IntersectWith ( other );
		}

		public bool IsProperSubsetOf ( IEnumerable<T> other ) => Collection.IsProperSubsetOf ( other );

		public bool IsProperSupersetOf ( IEnumerable<T> other ) => Collection.IsProperSupersetOf ( other );

		public bool IsSubsetOf ( IEnumerable<T> other ) => Collection.IsSubsetOf ( other );

		public bool IsSupersetOf ( IEnumerable<T> other ) => Collection.IsSupersetOf ( other );

		public bool Overlaps ( IEnumerable<T> other ) => Collection.Overlaps ( other );

		public bool SetEquals ( IEnumerable<T> other ) => Collection.SetEquals ( other );

		public void SymmetricExceptWith ( IEnumerable<T> other )
		{
			Collection.SymmetricExceptWith ( other );
		}

		public void UnionWith ( IEnumerable<T> other )
		{
			Collection.UnionWith ( other );
		}

		public bool AddOrUpdate ( T item )
		{
			var result = Collection.Remove ( item );
			Collection.Add ( item );
			Save ( );

			return !result;
		}
	}
}