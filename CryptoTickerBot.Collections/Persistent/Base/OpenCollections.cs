using System.Collections.Concurrent;

namespace CryptoTickerBot.Collections.Persistent.Base
{
	internal static class OpenCollections
	{
		public static readonly ConcurrentDictionary<string, IPersistentCollection> Data =
			new ConcurrentDictionary<string, IPersistentCollection> ( );
	}
}