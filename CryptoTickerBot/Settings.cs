using System.IO;
using Newtonsoft.Json;
using NLog;

// ReSharper disable CollectionNeverUpdated.Global

namespace CryptoTickerBot
{
	public sealed class Settings
	{
		private const string SETTINGSFILE = "CoreSettings.json";
		private static readonly object LoadLock;

		public static Settings Instance { get; private set; }

		static Settings ( )
		{
			LoadLock = new object ( );
			Instance = new Settings ( );
			Load ( );
			Save ( );

			LogManager.Configuration.Variables["DataSource"] = Data.Settings.Instance.DataSource;
		}

		private Settings ( )
		{
		}

		public static void Save ( )
		{
			lock ( LoadLock )
				File.WriteAllText ( SETTINGSFILE, JsonConvert.SerializeObject ( Instance, Formatting.Indented ) );
		}

		public static void Load ( )
		{
			lock ( LoadLock )
			{
				if ( File.Exists ( SETTINGSFILE ) )
					Instance = JsonConvert.DeserializeObject<Settings> ( File.ReadAllText ( SETTINGSFILE ) );
			}
		}

		#region Properties
		public string FixerApiKey { get; set; }
		#endregion Properties
	}
}