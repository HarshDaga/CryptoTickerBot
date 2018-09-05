using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CryptoTickerBot.Core.Converters
{
	public class DecimalConverter : JsonConverter
	{
		public override bool CanConvert ( Type objectType ) =>
			objectType == typeof ( decimal ) || objectType == typeof ( decimal? );

		public override object ReadJson ( JsonReader reader,
		                                  Type objectType,
		                                  object existingValue,
		                                  JsonSerializer serializer )
		{
			var token = JToken.Load ( reader );

			if ( token.Type == JTokenType.Float || token.Type == JTokenType.Integer )
				return token.ToObject<decimal> ( );

			if ( token.Type == JTokenType.String )
				return decimal.Parse ( token.ToString ( ), NumberStyles.Number | NumberStyles.AllowExponent );

			if ( token.Type == JTokenType.Null && objectType == typeof ( decimal? ) )
				return null;

			throw new JsonSerializationException ( $"Unexpected token type: {token.Type}" );
		}

		public override void WriteJson ( JsonWriter writer,
		                                 object value,
		                                 JsonSerializer serializer )
		{
			throw new NotImplementedException ( );
		}
	}
}