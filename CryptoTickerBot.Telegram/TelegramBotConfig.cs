using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Data.Configs;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace CryptoTickerBot.Telegram
{
	public class TelegramBotConfig : IConfig<TelegramBotConfig>
	{
		public string ConfigFileName { get; } = "TelegramBot";
		public string ConfigFolderName { get; } = "Configs";

		public string BotToken { get; set; }
		public int RetryLimit { get; set; } = 5;
		public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds ( 5 );

		public TelegramBotConfig RestoreDefaults ( ) =>
			new TelegramBotConfig
			{
				BotToken = BotToken
			};

		public bool TryValidate ( out IList<Exception> exceptions )
		{
			exceptions = new List<Exception> ( );

			if ( string.IsNullOrEmpty ( BotToken ) )
				exceptions.Add ( new ArgumentException ( "Bot Token missing", nameof ( BotToken ) ) );

			return !exceptions.Any ( );
		}
	}
}