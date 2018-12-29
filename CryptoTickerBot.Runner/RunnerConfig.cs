using CryptoTickerBot.Data.Configs;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace CryptoTickerBot.Runner
{
	public class RunnerConfig : IConfig<RunnerConfig>
	{
		public string ConfigFileName { get; } = "Runner";
		public string ConfigFolderName { get; } = "Configs";

		public bool EnableConsoleService { get; set; } = false;
		public bool EnableGoogleSheetsService { get; set; } = true;
		public bool EnableTelegramService { get; set; } = true;
		public RunnerConfig RestoreDefaults ( ) => this;
	}
}