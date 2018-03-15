using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace CryptoTickerBot.WebSocket.Messages
{
	public enum WssMessageType
	{
		Command,
		Subscribe,
		Unsubscribe
	}

	public class WebSocketIncomingMessage :
		IComparable<WebSocketIncomingMessage>,
		IComparable,
		IEquatable<WebSocketIncomingMessage>
	{
		public const string Format =
			"{  \"type\": {0 = Command, 1 = Subscribe, 2 = Unsubscribe}," +
			"  \"name\": \"<Command/Sub>\",  \"data\": <params>}";

		[JsonProperty ( "type" )]
		[DefaultValue ( WssMessageType.Command )]
		public WssMessageType Type { get; set; }

		[JsonProperty ( "name", Required = Required.Always )]
		public string Name { get; set; }

		[JsonProperty ( "data" )]
		public dynamic Data { get; set; }

		public int CompareTo ( object obj )
		{
			if ( ReferenceEquals ( null, obj ) ) return 1;
			if ( ReferenceEquals ( this, obj ) ) return 0;
			if ( !( obj is WebSocketIncomingMessage ) )
				throw new ArgumentException ( $"Object must be of type {nameof ( WebSocketIncomingMessage )}" );
			return CompareTo ( (WebSocketIncomingMessage) obj );
		}

		public int CompareTo ( WebSocketIncomingMessage other )
		{
			if ( ReferenceEquals ( this, other ) ) return 0;
			if ( ReferenceEquals ( null, other ) ) return 1;
			return string.Compare ( Name, other.Name, StringComparison.OrdinalIgnoreCase );
		}

		public bool Equals ( WebSocketIncomingMessage other )
		{
			if ( ReferenceEquals ( null, other ) ) return false;
			if ( ReferenceEquals ( this, other ) ) return true;
			return string.Equals ( Name, other.Name, StringComparison.OrdinalIgnoreCase );
		}

		public static bool operator < ( WebSocketIncomingMessage left, WebSocketIncomingMessage right ) =>
			Comparer<WebSocketIncomingMessage>.Default.Compare ( left, right ) < 0;

		public static bool operator > ( WebSocketIncomingMessage left, WebSocketIncomingMessage right ) =>
			Comparer<WebSocketIncomingMessage>.Default.Compare ( left, right ) > 0;

		public static bool operator <= ( WebSocketIncomingMessage left, WebSocketIncomingMessage right ) =>
			Comparer<WebSocketIncomingMessage>.Default.Compare ( left, right ) <= 0;

		public static bool operator >= ( WebSocketIncomingMessage left, WebSocketIncomingMessage right ) =>
			Comparer<WebSocketIncomingMessage>.Default.Compare ( left, right ) >= 0;

		public override bool Equals ( object obj )
		{
			if ( ReferenceEquals ( null, obj ) ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;
			if ( obj.GetType ( ) != GetType ( ) ) return false;
			return Equals ( (WebSocketIncomingMessage) obj );
		}

		public override int GetHashCode ( ) => Name != null ? Name.GetHashCode ( ) : 0;

		public static bool operator == ( WebSocketIncomingMessage left, WebSocketIncomingMessage right ) =>
			Equals ( left, right );

		public static bool operator != ( WebSocketIncomingMessage left, WebSocketIncomingMessage right ) =>
			!Equals ( left, right );

		public static bool operator == ( WebSocketIncomingMessage left, string right ) =>
			left?.Name?.Equals ( right, StringComparison.OrdinalIgnoreCase ) ?? false;

		public static bool operator != ( WebSocketIncomingMessage left, string right ) =>
			!( left == right );

		public static bool operator == ( string left, WebSocketIncomingMessage right ) =>
			right == left;

		public static bool operator != ( string left, WebSocketIncomingMessage right ) =>
			!( right == left );
	}

	public class WebSocketCommand :
		IComparable<WebSocketCommand>,
		IComparable,
		IEquatable<WebSocketCommand>
	{
		[JsonProperty ( "event" )]
		public string Event { get; set; }

		public int CompareTo ( object obj )
		{
			if ( ReferenceEquals ( null, obj ) )
				return 1;
			if ( ReferenceEquals ( this, obj ) )
				return 0;
			if ( !( obj is WebSocketCommand ) )
				throw new ArgumentException ( $"Object must be of type {nameof ( WebSocketCommand )}" );
			return CompareTo ( (WebSocketCommand) obj );
		}

		public int CompareTo ( WebSocketCommand other )
		{
			if ( ReferenceEquals ( this, other ) )
				return 0;
			if ( ReferenceEquals ( null, other ) )
				return 1;
			return string.Compare ( Event, other.Event, StringComparison.OrdinalIgnoreCase );
		}

		public bool Equals ( WebSocketCommand other )
		{
			if ( ReferenceEquals ( null, other ) )
				return false;
			if ( ReferenceEquals ( this, other ) )
				return true;
			return string.Equals ( Event, other.Event, StringComparison.OrdinalIgnoreCase );
		}

		public static bool operator < ( WebSocketCommand left, WebSocketCommand right ) =>
			Comparer<WebSocketCommand>.Default.Compare ( left, right ) < 0;

		public static bool operator > ( WebSocketCommand left, WebSocketCommand right ) =>
			Comparer<WebSocketCommand>.Default.Compare ( left, right ) > 0;

		public static bool operator <= ( WebSocketCommand left, WebSocketCommand right ) =>
			Comparer<WebSocketCommand>.Default.Compare ( left, right ) <= 0;

		public static bool operator >= ( WebSocketCommand left, WebSocketCommand right ) =>
			Comparer<WebSocketCommand>.Default.Compare ( left, right ) >= 0;

		public override bool Equals ( object obj )
		{
			if ( ReferenceEquals ( null, obj ) )
				return false;
			if ( ReferenceEquals ( this, obj ) )
				return true;
			if ( obj.GetType ( ) != GetType ( ) )
				return false;
			return Equals ( (WebSocketCommand) obj );
		}

		public override int GetHashCode ( ) => Event != null ? Event.GetHashCode ( ) : 0;

		public static bool operator == ( WebSocketCommand left, WebSocketCommand right ) =>
			Equals ( left, right );

		public static bool operator != ( WebSocketCommand left, WebSocketCommand right ) =>
			!Equals ( left, right );

		public static bool operator == ( WebSocketCommand left, string right ) =>
			left?.Event?.Equals ( right, StringComparison.OrdinalIgnoreCase ) ?? false;

		public static bool operator != ( WebSocketCommand left, string right ) =>
			!( left == right );

		public static bool operator == ( string left, WebSocketCommand right ) =>
			right == left;

		public static bool operator != ( string left, WebSocketCommand right ) =>
			!( right == left );
	}
}