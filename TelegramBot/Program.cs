using System;
using System.Threading;
using CryptoTickerBot.Core;
using NLog;
using TelegramBot.Core;

namespace TelegramBot
{
	public class Program
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public static void Main ( )
		{
			AppDomain.CurrentDomain.UnhandledException += ( sender, args ) =>
				Logger.Error ( args );

			Console.Title = "Crypto Ticker Telegram Bot";

			var ctb = Bot.CreateAndStart (
				new CancellationTokenSource ( ),
				Settings.Instance.ApplicationName,
				Settings.Instance.SheetName,
				Settings.Instance.SheetId,
				Settings.Instance.SheetsRanges
			);

			var teleBot = new TeleBot ( Settings.Instance.BotToken, ctb );
			teleBot.Start ( );
		}
	}
}