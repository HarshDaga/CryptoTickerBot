using System.Drawing;
using System.Threading.Tasks;
using Colorful;
using CryptoTickerBot.Core;
using CryptoTickerBot.Core.Configs;

namespace CryptoTickerBot.CUI
{
	public class Program
	{
		public static async Task Main ( string[] args )
		{
			ConfigManager<CoreConfig>.Reset ( );

			var styleSheet = new StyleSheet ( Color.White );
			styleSheet.AddStyle ( @"Highest Bid = (0|[1-9][\d,]*)?(\.\d+)?(?<=\d)", Color.GreenYellow );
			styleSheet.AddStyle ( @"Lowest Ask = (0|[1-9][\d,]*)?(\.\d+)?(?<=\d)", Color.OrangeRed );

			var bot = new Bot ( );

			var consoleLock = new object ( );

			bot.Changed += ( ex,
			                 coin ) =>
			{
				lock ( consoleLock )
				{
					Console.Write ( $"{ex.Name,-12}", Color.DodgerBlue );
					Console.WriteLineStyled ( $" | {coin}", styleSheet );
				}

				return Task.CompletedTask;
			};

			await bot.StartAsync ( );

			Console.ReadLine ( );
		}
	}
}