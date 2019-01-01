using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Colorful;
using CryptoTickerBot.Core;
using CryptoTickerBot.CUI;
using CryptoTickerBot.Data.Configs;
using CryptoTickerBot.GoogleSheets;
using CryptoTickerBot.Telegram;
using NLog;

namespace CryptoTickerBot.Runner
{
	public class Program
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private static readonly ManualResetEvent QuitEvent = new ManualResetEvent ( false );

		private static RunnerConfig RunnerConfig => ConfigManager<RunnerConfig>.Instance;

		private static bool HasExceptions<TConfig> ( ) where TConfig : IConfig<TConfig>, new ( )
		{
			if ( ConfigManager<TConfig>.TryValidate ( out var exceptions ) )
				return false;

			foreach ( var exception in exceptions )
				Logger.Error ( exception );

			return true;
		}

		private static bool ValidateConfigs ( )
		{
			if ( HasExceptions<CoreConfig> ( ) )
				return false;

			if ( HasExceptions<RunnerConfig> ( ) )
				return false;

			if ( RunnerConfig.EnableGoogleSheetsService && HasExceptions<SheetsConfig> ( ) )
				return false;

			if ( RunnerConfig.EnableTelegramService && HasExceptions<TelegramBotConfig> ( ) )
				return false;

			return true;
		}

		[SuppressMessage ( "ReSharper", "AsyncConverter.AsyncMethodNamingHighlighting" )]
		public static async Task Main ( )
		{
			Console.CancelKeyPress += ( sender,
			                            eArgs ) =>
			{
				QuitEvent.Set ( );
				eArgs.Cancel = true;
			};

			if ( !ValidateConfigs ( ) )
				return;

			var bot = new Bot ( );

			if ( RunnerConfig.EnableGoogleSheetsService )
			{
				var config = ConfigManager<SheetsConfig>.Instance;

				var service = new GoogleSheetsUpdaterService ( config );

				service.Update += updaterService =>
				{
					Logger.Debug ( $"Sheets Updated @ {service.LastUpdate}" );
					return Task.CompletedTask;
				};

				await bot.AttachAsync ( service ).ConfigureAwait ( false );
			}

			if ( RunnerConfig.EnableConsoleService )
				await bot.AttachAsync ( new ConsolePrintService ( ) ).ConfigureAwait ( false );

			if ( RunnerConfig.EnableTelegramService )
			{
				var teleService = new TelegramBotService ( ConfigManager<TelegramBotConfig>.Instance );
				await bot.AttachAsync ( teleService ).ConfigureAwait ( false );
			}

			await bot.StartAsync ( ).ConfigureAwait ( false );

			QuitEvent.WaitOne ( );
		}
	}
}