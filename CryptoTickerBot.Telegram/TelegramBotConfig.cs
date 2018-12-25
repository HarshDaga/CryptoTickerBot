using System;
using CryptoTickerBot.Data.Configs;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace CryptoTickerBot.Telegram
{
	public class TelegramBotConfig : IConfig
	{
		public string ConfigFileName { get; } = "TelegramBot";
		public string ConfigFolderName { get; } = "Configs";

		public string BotToken { get; set; }
		public int OwnerId { get; set; }
		public int RetryLimit { get; set; } = 5;
		public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds ( 5 );
	}
}