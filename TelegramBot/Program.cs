using System;
using CryptoTickerBot;
using CryptoTickerBot.GoogleSheets;
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

			LogManager.Configuration.Variables["DataSource"] =
				CryptoTickerBot.Data.Settings.Instance.DataSource;

			Console.Title = "Crypto Ticker Telegram Bot";

			var ctb = CryptoTickerBotCore.CreateAndStart ( );
			StartGoogleSheetUpdater ( ctb );

			var teleBot = new TeleBot ( Settings.Instance.BotToken, ctb );
			teleBot.Start ( );
			teleBot.Restart += bot => StartGoogleSheetUpdater ( bot.Ctb );

			Console.ReadLine ( );
		}

		public static GoogleSheetsUpdater StartGoogleSheetUpdater ( CryptoTickerBotCore ctb ) =>
			GoogleSheetsUpdater.Build (
				ctb,
				Settings.Instance.ApplicationName,
				Settings.Instance.SheetName,
				Settings.Instance.SheetId,
				Settings.Instance.SheetsRanges,
				Settings.Instance.SheetUpdateFrequency
			);
	}
}