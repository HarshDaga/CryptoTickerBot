using Newtonsoft.Json;

namespace CryptoTickerBot.Data.Configs
{
	public interface IConfig
	{
		[JsonIgnore]
		string ConfigFileName { get; }
	}
}