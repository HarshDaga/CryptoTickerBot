using System.Threading.Tasks;
using Colorful;
using CryptoTickerBot.Core;
using CryptoTickerBot.Domain.Configs;
using CryptoTickerBot.GoogleSheets;
using CryptoTickerBot.Telegram;
using NLog;

namespace CryptoTickerBot.Runner
{
	public class Program
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public static async Task Main ( string[] args )
		{
			//ConfigManager<CoreConfig>.Reset ( );

			var bot = new Bot ( );

			var config = ConfigManager<SheetsConfig>.Instance;

			var service = new GoogleSheetsUpdaterService ( config );

			service.Update += updaterService =>
			{
				Console.WriteLine ( $"Sheets Updated @ {service.LastUpdate}" );
				return Task.CompletedTask;
			};

			//await bot.Attach ( service );
			//await bot.Attach ( new ConsolePrintService ( ) );

			await bot.StartAsync ( );

			var teleService = new TelegramBotService ( ConfigManager<BotConfig>.Instance );
			await bot.Attach ( teleService );

			Console.ReadLine ( );
		}
	}
}