using System;
using System.Collections.Generic;
using CryptoTickerBot.Collections.Persistent.Base;

namespace CryptoTickerBot.Collections.Persistent
{
	public sealed class PersistentDictionary<TKey, TValue> :
		PersistentCollection<KeyValuePair<TKey, TValue>, Dictionary<TKey, TValue>>,
		IDictionary<TKey, TValue>
	{
		public TValue this [ TKey key ]
		{
			get => Collection[key];
			set
			{
				Collection[key] = value;
				Save ( );
			}
		}

		public ICollection<TKey> Keys => Collection.Keys;

		public ICollection<TValue> Values => Collection.Values;

		private PersistentDictionary ( string fileName,
		                               TimeSpan flushInterval )
			: base ( fileName, DefaultSerializerSettings, flushInterval )
		{
		}

		public static PersistentDictionary<TKey, TValue> Build ( string fileName ) =>
			Build ( fileName, DefaultFlushInterval );

		public static PersistentDictionary<TKey, TValue> Build ( string fileName,
		                                                         TimeSpan flushInterval )
		{
			var collection = GetOpenCollection<PersistentDictionary<TKey, TValue>> ( fileName );

			return collection ?? new PersistentDictionary<TKey, TValue> ( fileName, flushInterval );
		}

		public void Add ( TKey key,
		                  TValue value )
		{
			Collection.Add ( key, value );
			Save ( );
		}

		public bool ContainsKey ( TKey key ) => Collection.ContainsKey ( key );

		public bool Remove ( TKey key )
		{
			var result = Collection.Remove ( key );
			Save ( );

			return result;
		}

		public bool TryGetValue ( TKey key,
		                          out TValue value ) =>
			Collection.TryGetValue ( key, out value );
	}
}