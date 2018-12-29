using Newtonsoft.Json;

namespace CryptoTickerBot.Data.Configs
{
	public interface IConfig<out TConfig> where TConfig : IConfig<TConfig>
	{
		[JsonIgnore]
		string ConfigFileName { get; }

		[JsonIgnore]
		string ConfigFolderName { get; }

		TConfig RestoreDefaults ( );
	}
}