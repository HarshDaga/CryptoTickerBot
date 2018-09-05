using Newtonsoft.Json;

namespace CryptoTickerBot.Core.Interfaces
{
	public interface IConfig
	{
		[JsonIgnore]
		string ConfigFileName { get; }
	}
}