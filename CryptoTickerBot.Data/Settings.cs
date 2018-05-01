using System.IO;
using Newtonsoft.Json;
using NLog;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

// ReSharper disable CollectionNeverUpdated.Global

namespace CryptoTickerBot.Data
{
	public sealed class Settings
	{
		private const string SETTINGSFILE = "Data.json";
		private static readonly object LoadLock;

		public static Settings Instance { get; private set; }

		static Settings ( )
		{
			LoadLock = new object ( );
			Instance = new Settings ( );
			Load ( );
			Save ( );

			LogManager.Configuration.Variables["DataSource"] = Instance.DataSource;
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

		public string DataSource { get; set; } = "SQLEXPRESS";
		public string InitialCatalog { get; set; } = "CryptoTickerBotDb";
		public string IntegratedSecurity { get; set; } = "SSPI";

		#endregion Properties
	}
}