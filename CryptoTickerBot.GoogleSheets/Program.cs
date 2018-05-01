using System;
using NLog;

namespace CryptoTickerBot.GoogleSheets
{
	public class Program
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public static void Main ( )
		{
			AppDomain.CurrentDomain.UnhandledException += ( sender, args ) =>
				Logger.Error ( args );

			LogManager.Configuration.Variables["DataSource"] = Data.Settings.Instance.DataSource;

			Console.Title = "Crypto Ticker Bot";

			var ctb = CryptoTickerBotCore.CreateAndStart ( );
			StartGoogleSheetUpdater ( ctb );

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