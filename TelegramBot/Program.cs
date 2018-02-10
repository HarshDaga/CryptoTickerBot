using System;
using CryptoTickerBot.Core;
using TelegramBot.CryptoTickerTeleBot;

namespace TelegramBot
{
	public class Program
	{
		public static void Main ( )
		{
			Console.Title = "Crypto Ticker Telegram Bot";
			var ctb = new Bot ( );
			ctb.Start ( );
			var teleBot = new TeleBot ( Settings.Instance.BotToken, ctb );
			teleBot.Start ( );
		}
	}
}