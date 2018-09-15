namespace CryptoTickerBot.Data.Configs
{
	public class DataConfig : IConfig
	{
		public string ConfigFileName { get; } = "DataConfig.json";

		public string HostName { get; set; } = "localhost";
		public int Port { get; set; } = 28015;
		public string DbName { get; set; } = "CryptoTickerBotDb";
	}
}