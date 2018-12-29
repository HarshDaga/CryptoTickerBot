using System;
using System.Collections.Generic;
using CryptoTickerBot.Collections.Persistent.Base;

namespace CryptoTickerBot.Collections.Persistent
{
	public sealed class PersistentList<T> : PersistentCollection<T, List<T>>, IList<T>
	{
		public T this [ int index ]
		{
			get => Collection[index];
			set
			{
				Collection[index] = value;
				Save ( );
			}
		}

		private PersistentList ( string fileName,
		                         TimeSpan flushInterval )
			: base ( fileName, DefaultSerializerSettings, flushInterval )
		{
		}

		public static PersistentList<T> Build ( string fileName ) =>
			Build ( fileName, DefaultFlushInterval );

		public static PersistentList<T> Build ( string fileName,
		                                        TimeSpan flushInterval )
		{
			var collection = GetOpenCollection<PersistentList<T>> ( fileName );

			return collection ?? new PersistentList<T> ( fileName, flushInterval );
		}

		public int IndexOf ( T item ) => Collection.IndexOf ( item );

		public void Insert ( int index,
		                     T item )
		{
			Collection.Insert ( index, item );
			Save ( );
		}

		public void RemoveAt ( int index )
		{
			Collection.RemoveAt ( index );
			Save ( );
		}
	}
}