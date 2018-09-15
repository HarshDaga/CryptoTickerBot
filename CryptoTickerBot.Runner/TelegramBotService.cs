using System.Threading.Tasks;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Telegram;

namespace CryptoTickerBot.Runner
{
	public class TelegramBotService : BotServiceBase
	{
		public TelegramBot TelegramBot { get; set; }
		public TelegramBotConfig TelegramBotConfig { get; set; }

		public TelegramBotService ( TelegramBotConfig telegramBotConfig )
		{
			TelegramBotConfig = telegramBotConfig;
		}

		public override async Task StartAsync ( )
		{
			TelegramBot = new TelegramBot ( TelegramBotConfig, Bot );

			await TelegramBot.StartAsync ( );
		}
	}
}