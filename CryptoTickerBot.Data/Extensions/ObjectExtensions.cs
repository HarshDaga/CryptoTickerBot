using Newtonsoft.Json;

namespace CryptoTickerBot.Data.Extensions
{
	public static class ObjectExtensions
	{
		public static string ToJson<T> ( this T value ) =>
			JsonConvert.SerializeObject ( value );

		public static string ToJson<T> ( this T value,
		                                 Formatting formatting,
		                                 JsonSerializerSettings settings ) =>
			JsonConvert.SerializeObject ( value, formatting, settings );

		public static string ToJson<T> ( this T value,
		                                 Formatting formatting,
		                                 params JsonConverter[] converters ) =>
			JsonConvert.SerializeObject ( value, formatting, converters );

		public static string ToJson<T> ( this T value,
		                                 params JsonConverter[] converters ) =>
			JsonConvert.SerializeObject ( value, converters );

		public static string ToJson<T> ( this T value,
		                                 Formatting formatting ) =>
			JsonConvert.SerializeObject ( value, formatting );

		public static string ToJson<T> ( this T value,
		                                 JsonSerializerSettings settings ) =>
			JsonConvert.SerializeObject ( value, settings );
	}
}