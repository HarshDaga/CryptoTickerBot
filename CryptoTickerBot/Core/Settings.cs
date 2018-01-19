using System.Collections.Generic;
using System.IO;
using CryptoTickerBot.Exchanges;
using Newtonsoft.Json;

namespace CryptoTickerBot.Core
{
	public class Settings
	{
		private static readonly object LoadLock;

		public static Settings Instance { get; private set; }

		private const string SETTINGSFILE = "Settings.json";

		#region Properties

		public Dictionary<CryptoExchange, string> SheetsRanges { get; set; } = new Dictionary<CryptoExchange, string>
		{
			[CryptoExchange.BitBay] = "A3:D6",
			[CryptoExchange.Koinex] = "A12:D15",
			[CryptoExchange.Binance] = "A20:D23",
			//[CryptoExchange.CoinDelta] = "A29:D32",
			[CryptoExchange.Coinbase] = "A37:D40",
			[CryptoExchange.Kraken] = "A29:D32",
		};

		public string ApplicationName { get; set; } = "Crypto Ticker Bot";

		public string SheetName { get; set; } = "Tickers";

		public string SheetId { get; set; }

		#endregion Properties

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
	}
}