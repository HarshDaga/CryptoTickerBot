using System.Collections.Generic;
using System.IO;
using CryptoTickerBot.Exchanges;
using Newtonsoft.Json;

namespace CryptoTickerBot.Core
{
	public class Settings
	{
		private static Settings instance;
		private static readonly object LoadLock;

		public static Settings Instance => instance;

		private const string SETTINGSFILE = "Settings.json";

		#region Defaults

		private static Dictionary<CryptoExchange, string> sheetsRanges = new Dictionary<CryptoExchange, string>
		{
			[CryptoExchange.BitBay] = "A3:D6",
			[CryptoExchange.Koinex] = "A12:D15",
			[CryptoExchange.Binance] = "A20:D23",
			//[CryptoExchange.CoinDelta] = "A29:D32",
			[CryptoExchange.Coinbase] = "A37:D40",
			[CryptoExchange.Kraken] = "A29:D32",
		};

		private static string applicationName = "Crypto Ticker Bot";
		private static string sheetName = "Tickers";
		private static string sheetId;

		#endregion Defaults

		#region Properties

		public Dictionary<CryptoExchange, string> SheetsRanges
		{
			get => sheetsRanges;
			set => sheetsRanges = value;
		}

		public string ApplicationName
		{
			get => applicationName;
			set => applicationName = value;
		}

		public string SheetName
		{
			get => sheetName;
			set => sheetName = value;
		}

		public string SheetId
		{
			get => sheetId;
			set => sheetId = value;
		}

		#endregion Properties

		static Settings ( )
		{
			LoadLock = new object ( );
			instance = new Settings ( );
			Load ( );
			Save ( );
		}

		public static void Save ( )
		{
			File.WriteAllText ( SETTINGSFILE, JsonConvert.SerializeObject ( instance, Formatting.Indented ) );
		}

		public static void Load ( )
		{
			lock ( LoadLock )
			{
				if ( File.Exists ( SETTINGSFILE ) )
					instance = JsonConvert.DeserializeObject<Settings> ( File.ReadAllText ( SETTINGSFILE ) );
			}
		}
	}
}