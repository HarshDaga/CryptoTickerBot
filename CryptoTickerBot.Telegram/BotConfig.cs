using System;
using CryptoTickerBot.Domain.Configs;

namespace CryptoTickerBot.Telegram
{
	public class BotConfig : IConfig
	{
		public string ConfigFileName { get; } = "BotConfig.json";

		public string BotToken { get; set; }
		public int OwnerId { get; set; }
		public int RetryLimit { get; set; } = 5;
		public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds ( 5 );
	}
}