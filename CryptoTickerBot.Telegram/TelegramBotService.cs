using System.Threading.Tasks;
using CryptoTickerBot.Core.Abstractions;

namespace CryptoTickerBot.Telegram
{
	public class TelegramBotService : BotServiceBase
	{
		public TelegramBot TelegramBot { get; set; }
		public TelegramBotConfig TelegramBotConfig { get; set; }

		public TelegramBotService ( TelegramBotConfig telegramBotConfig )
		{
			TelegramBotConfig = telegramBotConfig;
		}

		public override Task StartAsync ( )
		{
			TelegramBot = new TelegramBot ( TelegramBotConfig, Bot );

			TelegramBot.Ctb.Start += async bot =>
				await TelegramBot.StartAsync ( );

			return Task.CompletedTask;
		}
	}
}