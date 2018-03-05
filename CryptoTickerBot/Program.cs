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
			AppDomain.CurrentDomain.UnhandledException += ( sender, args ) =>
				Logger.Error ( args );

			Console.Title = Settings.Instance.ApplicationName;
			Logger.Info ( $"Started {Settings.Instance.ApplicationName}" );

			Bot.CreateAndStart (
				new CancellationTokenSource ( ),
				Settings.Instance.ApplicationName,
				Settings.Instance.SheetName,
				Settings.Instance.SheetId,
				Settings.Instance.SheetsRanges
			);

			Thread.Sleep ( int.MaxValue );
		}
	}
}