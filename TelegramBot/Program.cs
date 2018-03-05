using System;
using CryptoTickerBot.Core;
using NLog;
using TelegramBot.CryptoTickerTeleBot;

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
			var ctb = new Bot ( );
			ctb.Start ( );
			var teleBot = new TeleBot ( Settings.Instance.BotToken, ctb );
			teleBot.Start ( );
		}
	}
}