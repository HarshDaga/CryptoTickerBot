using CryptoTickerBot.Data.Configs;

namespace CryptoTickerBot.Runner
{
	public class RunnerConfig : IConfig
	{
		public string ConfigFileName { get; } = "RunnerConfig";

		public bool EnableConsoleService { get; set; } = false;
		public bool EnableGoogleSheetsService { get; set; } = true;
		public bool EnableTelegramService { get; set; } = true;
	}
}