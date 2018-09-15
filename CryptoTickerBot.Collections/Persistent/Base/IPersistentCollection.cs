using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CryptoTickerBot.Collections.Persistent.Base
{
	public interface IPersistentCollection : IDisposable
	{
		string FileName { get; }
		JsonSerializerSettings SerializerSettings { get; set; }
		TimeSpan FlushInterval { get; }

		event SaveDelegate OnSave;
		event LoadDelegate OnLoad;
		event ErrorDelegate OnError;

		void ForceSave ( );
		void Save ( );
		bool Load ( );
	}

	public interface IPersistentCollection<T> : IPersistentCollection, ICollection<T>
	{
	}

	public delegate void SaveDelegate ( IPersistentCollection collection );

	public delegate void LoadDelegate ( IPersistentCollection collection );

	public delegate void ErrorDelegate ( IPersistentCollection collection,
	                                     Exception exception );
}