namespace TelegramBot
{
	public class Program
	{
		public static void Main ( )
		{
			var teleBot = new CryptoTickerTeleBot.TeleBot ( Settings.Instance.BotToken );
			teleBot.Start ( );
		}
	}
}