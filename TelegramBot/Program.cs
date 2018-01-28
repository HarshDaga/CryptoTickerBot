using TelegramBot.CryptoTickerTeleBot;

namespace TelegramBot
{
	public class Program
	{
		public static void Main ( )
		{
			var teleBot = new TeleBot ( Settings.Instance.BotToken );
			teleBot.Start ( );
		}
	}
}