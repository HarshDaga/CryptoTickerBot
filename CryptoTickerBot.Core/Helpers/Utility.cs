using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace CryptoTickerBot.Core.Helpers
{
	public static class Utility
	{
		static Utility ( )
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		}

		public static async Task<string> DownloadWebPageAsync ( string url )
		{
			var client = new WebClient ( );

			client.Headers.Add ( "user-agent",
			                     "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.139 Safari/537.36Mozilla/5.0" );

			var data = await client.OpenReadTaskAsync ( url ).ConfigureAwait ( false );

			if ( data == null ) return null;

			var reader = new StreamReader ( data );
			var s = await reader.ReadToEndAsync ( ).ConfigureAwait ( false );
			return s;
		}
	}
}