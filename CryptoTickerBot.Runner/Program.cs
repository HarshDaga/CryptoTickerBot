using System.Threading.Tasks;
using CryptoTickerBot.Core;
using CryptoTickerBot.CUI;
using CryptoTickerBot.Domain.Configs;
using CryptoTickerBot.GoogleSheets;
using NLog;
using Console = Colorful.Console;

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

			await bot.Attach ( service );
			await bot.Attach ( new ConsolePrintService ( ) );

			await bot.StartAsync ( );

			Console.ReadLine ( );
		}
	}
}