using System.Collections.Immutable;

namespace CryptoTickerBot.Collections.Persistent.Base
{
	internal static class OpenCollections
	{
		public static ImmutableDictionary<string, IPersistentCollection> Data =
			ImmutableDictionary<string, IPersistentCollection>.Empty;

		public static bool Add ( IPersistentCollection collection )
		{
			if ( Data.ContainsKey ( collection.FileName ) )
				return false;

			Data = Data.Add ( collection.FileName, collection );
			return true;
		}

		public static bool TryOpen ( string fileName,
		                             out IPersistentCollection opened )
		{
			if ( Data.TryGetValue ( fileName, out var existing ) )
			{
				opened = existing;
				return true;
			}

			opened = null;
			return false;
		}

		public static bool Remove ( string fileName )
		{
			if ( !Data.ContainsKey ( fileName ) )
				return false;

			Data = Data.Remove ( fileName );
			return true;
		}
	}
}