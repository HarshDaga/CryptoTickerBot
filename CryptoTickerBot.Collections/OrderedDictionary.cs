using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

// ReSharper disable InheritdocConsiderUsage

namespace CryptoTickerBot.Collections
{
	/// <summary>
	///     A dictionary that remembers the order that keys were first inserted. If a new entry overwrites an existing entry,
	///     the original insertion position is left unchanged. Deleting an entry and reinserting it will move it to the end.
	/// </summary>
	/// <typeparam name="TKey">The type of keys. Musn't be <see cref="int" /></typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	public sealed class OrderedDictionary<TKey, TValue> :
		IDictionary<TKey, TValue>
	{
		/// <summary>
		///     The number of elements in the dictionary.
		/// </summary>
		public int Count => fDictionary.Count;

		/// <summary>
		///     This dictionary is not read only.
		/// </summary>
		public bool IsReadOnly => false;

		/// <summary>
		///     The keys of the dictionary, in order.
		/// </summary>
		public ICollection<TKey> Keys => fKeys.AsReadOnly ( );

		/// <summary>
		///     The values in the dictionary, in order.
		/// </summary>
		public ICollection<TValue> Values
		{
			get { return fKeys.Select ( key => fDictionary[key] ).ToArray ( ); }
		}

		/// <summary>
		///     The value at the given index.
		/// </summary>
		public TValue this [ int index ]
		{
			get
			{
				var key = fKeys[index];
				return fDictionary[key];
			}
			set
			{
				var key = fKeys[index];
				fDictionary[key] = value;
			}
		}

		/// <summary>
		///     The value under the given key. New entries are added at the end of the order. Updating an existing entry does not
		///     change its position.
		/// </summary>
		public TValue this [ TKey key ]
		{
			get => fDictionary[key];
			set
			{
				if ( !fDictionary.ContainsKey ( key ) ) fKeys.Add ( key );

				fDictionary[key] = value;
			}
		}

		/// <summary>
		///     An unordered dictionary of key pairs.
		/// </summary>
		private readonly Dictionary<TKey, TValue> fDictionary;

		/// <summary>
		///     The keys of the dictionary in the exposed order.
		/// </summary>
		private readonly List<TKey> fKeys;

		/// <summary>
		///     A dictionary that remembers the order that keys were first inserted. If a new entry overwrites an existing entry,
		///     the original insertion position is left unchanged. Deleting an entry and reinserting it will move it to the end.
		/// </summary>
		public OrderedDictionary ( )
		{
			if ( typeof ( TKey ) == typeof ( int ) )
				throw new NotSupportedException (
					"Integer-like type is not appropriate for keys in an ordered dictionary - accessing values by key or index would be confusing." );

			fDictionary = new Dictionary<TKey, TValue> ( );
			fKeys       = new List<TKey> ( );
		}

		/// <summary>
		///     Find the position of an element by key. Returns -1 if the dictionary does not contain an element with the given
		///     key.
		/// </summary>
		public int IndexOf ( TKey key ) => fKeys.IndexOf ( key );

		/// <summary>
		///     Remove the element at the given index.
		/// </summary>
		public void RemoveAt ( int index )
		{
			var key = fKeys[index];
			fDictionary.Remove ( key );
			fKeys.RemoveAt ( index );
		}

		/// <summary>
		///     Test whether there is an element with the given key.
		/// </summary>
		public bool ContainsKey ( TKey key ) => fDictionary.ContainsKey ( key );

		/// <summary>
		///     Try to get a value from the dictionary, by key. Returns false if there is no element with the given key.
		/// </summary>
		public bool TryGetValue ( TKey key,
		                          out TValue value ) => fDictionary.TryGetValue ( key, out value );

		/// <summary>
		///     Insert an element at the given index.
		/// </summary>
		public void Insert ( int index,
		                     TKey key,
		                     TValue value )
		{
			// Dictionary operation first, so exception thrown if key already exists.
			fDictionary.Add ( key, value );
			fKeys.Insert ( index, key );
		}

		/// <summary>
		///     Add an element to the dictionary.
		/// </summary>
		public void Add ( TKey key,
		                  TValue value )
		{
			// Dictionary operation first, so exception thrown if key already exists.
			fDictionary.Add ( key, value );
			fKeys.Add ( key );
		}

		/// <summary>
		///     Add an element to the dictionary.
		/// </summary>
		public void Add ( KeyValuePair<TKey, TValue> pair )
		{
			Add ( pair.Key, pair.Value );
		}

		/// <summary>
		///     Test whether the dictionary contains an element equal to that given.
		/// </summary>
		public bool Contains ( KeyValuePair<TKey, TValue> pair ) => fDictionary.Contains ( pair );

		/// <summary>
		///     Remove a key-value pair from the dictionary. Return true if pair was successfully removed. Otherwise, if the pair
		///     was not found, return false.
		/// </summary>
		public bool Remove ( KeyValuePair<TKey, TValue> pair )
		{
			if ( Contains ( pair ) )
			{
				Remove ( pair.Key );
				return true;
			}

			return false;
		}

		/// <summary>
		///     Remove the element with the given key key. If there is no element with the key, no exception is thrown.
		/// </summary>
		public bool Remove ( TKey key )
		{
			var wasInDictionary = fDictionary.Remove ( key );
			var wasInKeys = fKeys.Remove ( key );
			Contract.Assume ( wasInDictionary == wasInKeys );
			return wasInDictionary;
		}

		/// <summary>
		///     Delete all elements from the dictionary.
		/// </summary>
		public void Clear ( )
		{
			fDictionary.Clear ( );
			fKeys.Clear ( );
		}

		/// <summary>
		///     Copy the elements of the dictionary to an array, starting at at the given index.
		/// </summary>
		public void CopyTo ( KeyValuePair<TKey, TValue>[] array,
		                     int index )
		{
			if ( array == null )
				throw new ArgumentNullException ( nameof ( array ) );

			if ( index < 0 )
				throw new ArgumentOutOfRangeException ( nameof ( index ), "Must be greater than or equal to zero." );

			if ( index + fDictionary.Count > array.Length )
				throw new ArgumentException ( "Array is too small", nameof ( array ) );

			foreach ( var pair in this )
			{
				array[index] = pair;
				index++;
			}
		}

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator ( ) =>
			Enumerate ( ).GetEnumerator ( );

		IEnumerator IEnumerable.GetEnumerator ( ) => Enumerate ( ).GetEnumerator ( );

		public OrderedDictionary<TKey, TValue> Clone ( )
		{
			var dict = new OrderedDictionary<TKey, TValue> ( );

			foreach ( var kp in this )
				dict.Add ( kp );

			return dict;
		}

		private IEnumerable<KeyValuePair<TKey, TValue>> Enumerate ( )
		{
			foreach ( var key in fKeys )
			{
				var value = fDictionary[key];
				yield return new KeyValuePair<TKey, TValue> ( key, value );
			}
		}

		/// <summary>
		///     Conditions that should be true at the end of every public method.
		/// </summary>
		[ContractInvariantMethod]
		private void ObjectInvariant ( )
		{
			Contract.Invariant ( fDictionary.Count == fKeys.Count,
			                     "Unordered dictionary and ordered key list should be the same length." );
		}
	}
}