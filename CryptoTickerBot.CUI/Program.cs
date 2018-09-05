using System.Threading.Tasks;
using Colorful;
using CryptoTickerBot.Core;

namespace CryptoTickerBot.CUI
{
	public class Program
	{
		public static async Task Main ( string[] args )
		{
			var bot = new Bot ( );

			var service = new ConsolePrintService ( );
			await bot.Attach ( service );

			await bot.StartAsync ( );

			await Task.Delay ( 10000 );
			await bot.Detach ( service );

			Console.ReadLine ( );
		}
	}
}