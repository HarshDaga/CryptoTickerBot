using System;
using CryptoTickerBot.Data.Configs;

namespace CryptoTickerBot.Telegram
{
	public class TelegramBotConfig : IConfig
	{
		public string ConfigFileName { get; } = "TelegramBotConfig";

		public string BotToken { get; set; }
		public int OwnerId { get; set; }
		public int RetryLimit { get; set; } = 5;
		public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds ( 5 );
	}
}