using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;

// ReSharper disable AssignNullToNotNullAttribute

namespace CryptoTickerBot.Helpers
{
	public static class WebRequests
	{
		public static string Get ( string uri )
		{
			var request = (HttpWebRequest) WebRequest.Create ( uri );
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			using ( var response = (HttpWebResponse) request.GetResponse ( ) )
			using ( var stream = response.GetResponseStream ( ) )
			using ( var reader = new StreamReader ( stream ) )
			{
				return reader.ReadToEnd ( );
			}
		}

		public static string Get ( Uri uri ) => Get ( uri.ToString ( ) );

		public static async Task<string> GetAsync ( string uri ) => await uri.GetStringAsync ( );

		public static async Task<string> GetAsync ( Uri uri ) => await GetAsync ( uri.ToString ( ) );

		public static string Post ( string uri, string data, string contentType, string method = "POST" )
		{
			var dataBytes = Encoding.UTF8.GetBytes ( data );

			var request = (HttpWebRequest) WebRequest.Create ( uri );
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			request.ContentLength = dataBytes.Length;
			request.ContentType = contentType;
			request.Method = method;

			using ( var requestBody = request.GetRequestStream ( ) )
			{
				requestBody.Write ( dataBytes, 0, dataBytes.Length );
			}

			using ( var response = (HttpWebResponse) request.GetResponse ( ) )
			using ( var stream = response.GetResponseStream ( ) )
			using ( var reader = new StreamReader ( stream ) )
			{
				return reader.ReadToEnd ( );
			}
		}

		public static async Task<string> PostAsync ( string uri, string data, string contentType, string method = "POST" )
		{
			var dataBytes = Encoding.UTF8.GetBytes ( data );

			var request = (HttpWebRequest) WebRequest.Create ( uri );
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			request.ContentLength = dataBytes.Length;
			request.ContentType = contentType;
			request.Method = method;

			using ( var requestBody = request.GetRequestStream ( ) )
			{
				await requestBody.WriteAsync ( dataBytes, 0, dataBytes.Length );
			}

			using ( var response = (HttpWebResponse) await request.GetResponseAsync ( ) )
			using ( var stream = response.GetResponseStream ( ) )
			using ( var reader = new StreamReader ( stream ) )
			{
				return await reader.ReadToEndAsync ( );
			}
		}
	}
}