using System.Threading;
using CryptoTickerBot.Core;

namespace CryptoTickerBot
{
	public class Program
	{
		public static void Main ( string[] args )
		{
			Bot.Start ( args );
			Thread.Sleep ( int.MaxValue );
		}
	}
}