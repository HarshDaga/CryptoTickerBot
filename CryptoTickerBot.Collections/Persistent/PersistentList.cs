using System;
using System.Collections.Generic;
using CryptoTickerBot.Collections.Persistent.Base;
using Newtonsoft.Json;

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

		public PersistentList ( string fileName )
			: base ( fileName )
		{
		}

		public PersistentList ( string fileName,
		                        JsonSerializerSettings serializerSettings )
			: base ( fileName, serializerSettings )
		{
		}

		public PersistentList ( string fileName,
		                        JsonSerializerSettings serializerSettings,
		                        TimeSpan flushInterval )
			: base ( fileName, serializerSettings, flushInterval )
		{
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