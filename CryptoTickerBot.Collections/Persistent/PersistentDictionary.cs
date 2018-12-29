using System;
using System.Collections.Generic;
using CryptoTickerBot.Collections.Persistent.Base;
using Newtonsoft.Json;

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

		public PersistentDictionary ( string fileName )
			: base ( fileName )
		{
		}

		public PersistentDictionary ( string fileName,
		                              JsonSerializerSettings serializerSettings )
			: base ( fileName, serializerSettings )
		{
		}

		public PersistentDictionary ( string fileName,
		                              JsonSerializerSettings serializerSettings,
		                              TimeSpan flushInterval )
			: base ( fileName, serializerSettings, flushInterval )
		{
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