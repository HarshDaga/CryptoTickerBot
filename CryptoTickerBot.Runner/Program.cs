using System.Threading.Tasks;
using Colorful;
using CryptoTickerBot.Core;
using CryptoTickerBot.Core.Configs;
using CryptoTickerBot.CUI;
using CryptoTickerBot.GoogleSheets;

namespace CryptoTickerBot.Runner
{
	public class Program
	{
		public static async Task Main ( string[] args )
		{
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