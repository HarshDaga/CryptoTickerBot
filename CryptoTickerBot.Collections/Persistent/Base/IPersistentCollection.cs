using System.Collections.Generic;
using Newtonsoft.Json;

namespace CryptoTickerBot.Collections.Persistent.Base
{
	public interface IPersistentCollection<T> : ICollection<T>
	{
		string FileName { get; }
		JsonSerializerSettings SerializerSettings { get; set; }

		event SaveDelegate<T> OnSave;
		event LoadDelegate<T> OnLoad;

		void Save ( );
		bool Load ( );
	}

	public delegate void SaveDelegate<T> ( IPersistentCollection<T> collection );

	public delegate void LoadDelegate<T> ( IPersistentCollection<T> collection );
}