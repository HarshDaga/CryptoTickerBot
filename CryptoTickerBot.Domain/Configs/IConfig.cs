using Newtonsoft.Json;

namespace CryptoTickerBot.Domain.Configs
{
	public interface IConfig
	{
		[JsonIgnore]
		string ConfigFileName { get; }
	}
}