using System;
using Newtonsoft.Json;

namespace CryptoTickerBot.Core.Converters
{
	public class UriConverter : JsonConverter
	{
		public override bool CanConvert ( Type objectType ) =>
			objectType == typeof ( Uri );

		public override object ReadJson ( JsonReader reader,
		                                  Type objectType,
		                                  object existingValue,
		                                  JsonSerializer serializer )
		{
			if ( reader.TokenType == JsonToken.String )
				return new Uri ( (string) reader.Value );

			if ( reader.TokenType == JsonToken.Null )
				return null;

			throw new InvalidOperationException (
				"Unhandled case for UriConverter. Check to see if this converter has been applied to the wrong serialization type." );
		}

		public override void WriteJson ( JsonWriter writer,
		                                 object value,
		                                 JsonSerializer serializer )
		{
			if ( null == value )
			{
				writer.WriteNull ( );
				return;
			}

			if ( value is Uri uri )
			{
				writer.WriteValue ( uri.OriginalString );
				return;
			}

			throw new InvalidOperationException (
				"Unhandled case for UriConverter. Check to see if this converter has been applied to the wrong serialization type." );
		}
	}
}