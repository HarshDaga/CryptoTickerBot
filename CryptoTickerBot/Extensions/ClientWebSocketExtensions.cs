using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket = WebSocketSharp.WebSocket;

namespace CryptoTickerBot.Extensions
{
	public static class ClientWebSocketExtensions
	{
		public static async Task SendStringAsync ( this ClientWebSocket ws, string str )
		{
			var bytesToSend = new ArraySegment<byte> ( Encoding.UTF8.GetBytes ( str ) );
			await ws.SendAsync ( bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None )
				.ConfigureAwait ( false );
		}

		public static async Task<string> ReceiveStringAsync ( this ClientWebSocket ws, int bufferSize = 10240 )
		{
			var bytesReceived = new ArraySegment<byte> ( new byte[bufferSize] );
			var result = await ws.ReceiveAsync ( bytesReceived, CancellationToken.None ).ConfigureAwait ( false );
			return Encoding.UTF8.GetString ( bytesReceived.Array ?? throw new OutOfMemoryException ( ), 0, result.Count );
		}

		public static async Task SendStringAsync ( this WebSocket ws, string str )
		{
			var finished = false;
			ws.SendAsync ( str, b => finished = b );
			await Task.Run ( ( ) =>
				{
					while ( !finished )
						Thread.Sleep ( 1 );
				}
			).ConfigureAwait ( false );
		}
	}
}