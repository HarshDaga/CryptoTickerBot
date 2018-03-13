using System;
using System.Diagnostics;

namespace CryptoTickerBot.WebSocket.Extensions
{
	public static class DecimalExtensions
	{
		[DebuggerStepThrough]
		public static decimal RoundOff ( this decimal d, int decimals = 2 ) =>
			Math.Round ( d, decimals );
	}
}