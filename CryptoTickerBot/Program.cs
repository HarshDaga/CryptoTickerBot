using System;
using System.Threading;
using CryptoTickerBot.Core;
using NLog;

namespace CryptoTickerBot
{
	public class Program
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public static void Main ( string[] args )
		{
			Console.Title = Settings.Instance.ApplicationName;
			Logger.Info ( $"Started {Settings.Instance.ApplicationName}" );
			Bot.Start ( args );
			Thread.Sleep ( int.MaxValue );
		}
	}
}