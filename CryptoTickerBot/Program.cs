using System;
using System.Threading;
using CryptoTickerBot.Core;
using NLog;

namespace CryptoTickerBot
{
	public class Program
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public static void Main ( )
		{
			Console.Title = Settings.Instance.ApplicationName;
			Logger.Info ( $"Started {Settings.Instance.ApplicationName}" );
			var ctb = new Bot ( );
			ctb.Start ( );
			Thread.Sleep ( int.MaxValue );
		}
	}
}