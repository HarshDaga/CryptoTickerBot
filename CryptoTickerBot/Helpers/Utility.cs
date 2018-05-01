using System.IO;
using System.Net;

namespace CryptoTickerBot.Helpers
{
	public static class Utility
	{
		public static string DownloadWebPage ( string url )
		{
			var client = new WebClient ( );

			client.Headers.Add ( "user-agent",
			                     "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.139 Safari/537.36Mozilla/5.0" );

			var data = client.OpenRead ( url );

			if ( data == null ) return null;

			var reader = new StreamReader ( data );
			var s = reader.ReadToEnd ( );
			return s;
		}
	}
}
