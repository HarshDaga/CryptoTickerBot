using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using Newtonsoft.Json;
using Polly;

// ReSharper disable StaticMemberInGenericType

namespace CryptoTickerBot.Collections.Persistent.Base
{
	public abstract class PersistentCollection<T, TCollection> :
		IPersistentCollection<T>
		where TCollection : ICollection<T>, new ( )
	{
		public static JsonSerializerSettings DefaultSerializerSettings { get; } = new JsonSerializerSettings
		{
			NullValueHandling      = NullValueHandling.Ignore,
			DefaultValueHandling   = DefaultValueHandling.IgnoreAndPopulate,
			Formatting             = Formatting.Indented,
			ReferenceLoopHandling  = ReferenceLoopHandling.Ignore,
			ObjectCreationHandling = ObjectCreationHandling.Replace
		};

		public static TimeSpan DefaultFlushInterval => TimeSpan.FromSeconds ( 1 );

		protected TCollection Collection { get; set; }

		public int Count => Collection.Count;

		public bool IsReadOnly => Collection.IsReadOnly;
		public string FileName { get; }

		public JsonSerializerSettings SerializerSettings { get; set; }
		public TimeSpan FlushInterval { get; }

		public int MaxRetryAttempts { get; set; } = 5;
		public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds ( 2 );

		protected readonly object FileLock = new object ( );

		private volatile bool saveRequested;
		private IDisposable disposable;

		protected PersistentCollection ( string fileName ) :
			this ( fileName, DefaultSerializerSettings, DefaultFlushInterval )
		{
		}

		protected PersistentCollection ( string fileName,
		                                 JsonSerializerSettings serializerSettings ) :
			this ( fileName, serializerSettings, DefaultFlushInterval )
		{
		}

		protected PersistentCollection ( string fileName,
		                                 JsonSerializerSettings serializerSettings,
		                                 TimeSpan flushInterval )
		{
			FileName           = fileName;
			SerializerSettings = serializerSettings;
			FlushInterval      = flushInterval;

			OpenCollections.Data[fileName] = this;

			if ( !Load ( ) )
			{
				Collection = new TCollection ( );
				Save ( );
			}

			disposable = Observable.Interval ( FlushInterval ).Subscribe ( l => ForceSave ( ) );
		}

		protected static bool TryOpenCollection ( string fileName,
		                                          out IPersistentCollection collection ) =>
			OpenCollections.Data.TryGetValue ( fileName, out collection );

		protected static TType GetOpenCollection<TType> ( string fileName )
			where TType : PersistentCollection<T, TCollection>
		{
			if ( TryOpenCollection ( fileName, out var collection ) )
			{
				if ( collection is TType result )
					return result;
				throw new InvalidCastException (
					$"{fileName} already has an open collection of type {collection.GetType ( )}" );
			}

			return null;
		}

		public event SaveDelegate OnSave;
		public event LoadDelegate OnLoad;
		public event ErrorDelegate OnError;

		public virtual IEnumerator<T> GetEnumerator ( ) => Collection.GetEnumerator ( );

		IEnumerator IEnumerable.GetEnumerator ( ) => ( (IEnumerable) Collection ).GetEnumerator ( );

		public virtual void Add ( T item )
		{
			Collection.Add ( item );
			Save ( );
		}

		public virtual void Clear ( )
		{
			Collection.Clear ( );
			Save ( );
		}

		public virtual bool Contains ( T item ) =>
			Collection.Contains ( item );

		public virtual void CopyTo ( T[] array,
		                             int arrayIndex )
		{
			Collection.CopyTo ( array, arrayIndex );
		}

		public virtual bool Remove ( T item )
		{
			var result = Collection.Remove ( item );
			Save ( );

			return result;
		}

		protected void OneTimeSave ( string json )
		{
			var fileInfo = new FileInfo ( FileName );
			fileInfo.Directory?.Create ( );
			File.WriteAllText ( FileName, json );
		}

		protected void InternalForceSave ( )
		{
			var json = JsonConvert.SerializeObject ( Collection );

			Policy
				.Handle<Exception> ( )
				.WaitAndRetry ( MaxRetryAttempts,
				                i => RetryInterval,
				                ( exception,
				                  span ) =>
					                OnError?.Invoke ( this, exception ) )
				.Execute ( ( ) => OneTimeSave ( json ) );
		}

		public void ForceSave ( )
		{
			if ( !saveRequested )
				return;

			lock ( FileLock )
			{
				try
				{
					InternalForceSave ( );

					OnSave?.Invoke ( this );
				}
				catch ( Exception e )
				{
					disposable.Dispose ( );
					disposable = null;
					OnError?.Invoke ( this, e );
				}
			}

			saveRequested = false;
		}

		public void Save ( )
		{
			saveRequested = true;
		}

		public bool Load ( )
		{
			if ( !File.Exists ( FileName ) )
				return false;

			lock ( FileLock )
			{
				Collection = JsonConvert.DeserializeObject<TCollection> ( File.ReadAllText ( FileName ),
				                                                          SerializerSettings );
				OnLoad?.Invoke ( this );
			}

			return true;
		}

		public void Dispose ( )
		{
			ForceSave ( );
			OpenCollections.Data.TryRemove ( FileName, out _ );
			disposable?.Dispose ( );
		}

		public virtual void AddWithoutSaving ( T item )
		{
			Collection.Add ( item );
		}

		public virtual void AddRange ( IEnumerable<T> collection )
		{
			foreach ( var item in collection )
				Collection.Add ( item );

			Save ( );
		}
	}
}