using System.Collections.Generic;
using System.IO;
using CryptoTickerBot.Data.Enums;
using Newtonsoft.Json;

// ReSharper disable CollectionNeverUpdated.Global

namespace CryptoTickerBot.Core
{
	public sealed class Settings
	{
		private const string SETTINGSFILE = "Settings.json";
		private static readonly object LoadLock;

		public static Settings Instance { get; private set; }

		static Settings ( )
		{
			LoadLock = new object ( );
			Instance = new Settings ( );
			Load ( );
			Save ( );
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

		public Dictionary<CryptoExchangeId, string> SheetsRanges { get; set; }
		public string ApplicationName { get; set; }
		public string SheetName { get; set; }
		public string SheetId { get; set; }

		#endregion Properties
	}
}