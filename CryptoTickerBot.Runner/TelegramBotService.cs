using System.Threading.Tasks;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Telegram;

namespace CryptoTickerBot.Runner
{
	public class TelegramBotService : BotServiceBase
	{
		public TelegramBot TelegramBot { get; set; }
		public BotConfig BotConfig { get; set; }

		public TelegramBotService ( BotConfig botConfig )
		{
			BotConfig = botConfig;
		}

		public override async Task StartAsync ( )
		{
			TelegramBot = new TelegramBot ( BotConfig, Bot );

			await TelegramBot.StartAsync ( );
		}
	}
}