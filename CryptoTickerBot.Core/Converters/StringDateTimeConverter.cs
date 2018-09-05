using System;
using Newtonsoft.Json;

namespace CryptoTickerBot.Core.Converters
{
	internal class StringDateTimeConverter : JsonConverter
	{
		public override bool CanConvert ( Type t ) => t == typeof ( DateTime ) || t == typeof ( DateTime? );

		public override object ReadJson ( JsonReader reader,
		                                  Type t,
		                                  object existingValue,
		                                  JsonSerializer serializer )
		{
			if ( reader.TokenType == JsonToken.Null ) return null;
			var value = serializer.Deserialize<string> ( reader );

			if ( long.TryParse ( value, out var l ) )
				return DateTimeOffset
					.FromUnixTimeSeconds ( l )
					.UtcDateTime;

			throw new Exception ( "Cannot un-marshall type long" );
		}

		public override void WriteJson ( JsonWriter writer,
		                                 object untypedValue,
		                                 JsonSerializer serializer )
		{
		}
	}
}