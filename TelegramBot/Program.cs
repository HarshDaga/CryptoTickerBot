namespace TelegramBot
{
	public class Program
	{
		public static void Main ( )
		{
			var teleBot = new CryptoTickerTeleBot ( Settings.Instance.BotToken );
			teleBot.Start ( );
		}
	}
}