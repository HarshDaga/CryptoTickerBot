using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CryptoTickerBot.Data.Configs
{
	public interface IConfig<out TConfig> where TConfig : IConfig<TConfig>
	{
		[JsonIgnore]
		string ConfigFileName { get; }

		[JsonIgnore]
		string ConfigFolderName { get; }

		bool Validate ( out IList<Exception> exceptions );

		TConfig RestoreDefaults ( );
	}
}