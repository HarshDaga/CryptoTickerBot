using Newtonsoft.Json;

namespace CryptoTickerBot.WebSocket.Messages
{
	public class WebSocketMessage
	{
		[JsonProperty ( "event" )]
		public string Event { get; set; }

		[JsonProperty ( "data" )]
		public dynamic Data { get; set; }

		public WebSocketMessage ( string @event = null, dynamic data = default ( dynamic ) )
		{
			Event = @event;
			Data  = data;
		}

		public string ToJson ( ) => JsonConvert.SerializeObject ( this );

		public static implicit operator string ( WebSocketMessage message ) =>
			message.ToJson ( );
	}

	public static class WebSocketMessageBuilder
	{
		public static WebSocketMessage Subscribe ( string eventName ) =>
			new WebSocketMessage ( "Subscribed", eventName );

		public static WebSocketMessage Unsubscribe ( string eventName ) =>
			new WebSocketMessage ( "Unsubscribed", eventName );

		public static WebSocketMessage Error ( dynamic data ) =>
			new WebSocketMessage ( "Error", data );
	}
}