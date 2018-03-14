using System;
using NLog;

namespace CryptoTickerBot
{
	public class Program
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public static void Main ( )
		{
			AppDomain.CurrentDomain.UnhandledException += ( sender, args ) =>
				Logger.Error ( args );

			Console.Title = "Crypto Ticker Bot";
			Logger.Info ( "Started Crypto Ticker Bot" );

			CryptoTickerBotCore.CreateAndStart ( );

			Console.ReadLine ( );
		}
	}
}