using System.Threading;
using System.Threading.Tasks;
using Colorful;
using CryptoTickerBot.Core;
using CryptoTickerBot.CUI;
using CryptoTickerBot.Data.Configs;
using CryptoTickerBot.GoogleSheets;
using CryptoTickerBot.Telegram;
using NLog;

//using CryptoTickerBot.GoogleSheets;

namespace CryptoTickerBot.Runner
{
	public class Program
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		private static RunnerConfig RunnerConfig => ConfigManager<RunnerConfig>.Instance;
		private static readonly ManualResetEvent QuitEvent = new ManualResetEvent ( false );

		public static async Task Main ( )
		{
			Console.CancelKeyPress += ( sender,
			                            eArgs ) =>
			{
				QuitEvent.Set ( );
				eArgs.Cancel = true;
			};
			//ConfigManager<CoreConfig>.Reset ( );

			var bot = new Bot ( );

			if ( RunnerConfig.EnableGoogleSheetsService )
			{
				var config = ConfigManager<SheetsConfig>.Instance;

				var service = new GoogleSheetsUpdaterService ( config );

				service.Update += updaterService =>
				{
					Console.WriteLine ( $"Sheets Updated @ {service.LastUpdate}" );
					return Task.CompletedTask;
				};

				await bot.Attach ( service );
			}

			if ( RunnerConfig.EnableConsoleService )
				await bot.Attach ( new ConsolePrintService ( ) );

			if ( RunnerConfig.EnableTelegramService )
			{
				var teleService = new TelegramBotService ( ConfigManager<TelegramBotConfig>.Instance );
				await bot.Attach ( teleService );
			}

			await bot.StartAsync ( );

			QuitEvent.WaitOne ( );
		}
	}
}