using Newtonsoft.Json;

namespace CryptoTickerBot.WebSocket.Extensions
{
	public static class StringExtensions
	{
		public static bool TryDeserialize<T> ( this string str, out T result )
		{
			try
			{
				result = JsonConvert.DeserializeObject<T> ( str );
				return true;
			}
			catch
			{
				result = default ( T );
				return false;
			}
		}
	}
}