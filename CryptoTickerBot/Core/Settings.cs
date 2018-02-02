using System;
using System.Collections.Generic;
using System.IO;
using CryptoTickerBot.Exchanges;
using Newtonsoft.Json;

// ReSharper disable CollectionNeverUpdated.Global

namespace CryptoTickerBot.Core
{
	public class Settings
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

		public Dictionary<CryptoExchange, string> SheetsRanges { get; set; }
		public string ApplicationName { get; set; }
		public string SheetName { get; set; }
		public string SheetId { get; set; }
		public TimeSpan HistorySpan { get; set; } = TimeSpan.FromMinutes ( 60 );

		#endregion Properties
	}
}