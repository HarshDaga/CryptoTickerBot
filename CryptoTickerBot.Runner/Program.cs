using System.Threading.Tasks;
using Colorful;
using CryptoTickerBot.Core;
using CryptoTickerBot.CUI;

namespace CryptoTickerBot.Runner
{
	public class Program
	{
		public static async Task Main ( string[] args )
		{
			var bot = new Bot ( );

			await bot.Attach ( new ConsolePrintService ( ) );

			await bot.StartAsync ( );

			Console.ReadLine ( );
		}
	}
}