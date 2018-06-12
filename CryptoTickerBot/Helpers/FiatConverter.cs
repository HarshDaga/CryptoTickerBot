using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Extensions;
using Flurl.Http;
using Newtonsoft.Json;
using NLog;

namespace CryptoTickerBot.Helpers
{
	public static class FiatConverter
	{
		public static readonly string TickerUrl = "http://data.fixer.io/api/latest";
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		private static readonly IDictionary<FiatCurrency, string> Map;
		private static readonly IDictionary<string, FiatCurrency> StringMap;

		public static Dictionary<FiatCurrency, decimal> UsdTo { get; private set; } =
			new Dictionary<FiatCurrency, decimal> ( );

		static FiatConverter ( )
		{
			Map = CultureInfo
				.GetCultures ( CultureTypes.AllCultures )
				.Where ( c => !c.IsNeutralCulture )
				.Select ( culture =>
				{
					try
					{
						return new RegionInfo ( culture.LCID );
					}
					catch
					{
						return null;
					}
				} )
				.Where (
					ri => ri != null && Enum.GetNames ( typeof ( FiatCurrency ) ).Contains ( ri.ISOCurrencySymbol ) )
				.GroupBy ( ri => ri.ISOCurrencySymbol )
				.ToDictionary (
					x => (FiatCurrency) Enum.Parse ( typeof ( FiatCurrency ), x.Key ),
					x => x.First ( ).CurrencySymbol
				);

			StringMap = new Dictionary<string, FiatCurrency> ( );
			foreach ( FiatCurrency fiat in Enum.GetValues ( typeof ( FiatCurrency ) ) )
				StringMap[fiat.ToString ( )] = fiat;

			TickerUrl += $"?access_key={Settings.Instance.FixerApiKey}";
			TickerUrl += "&symbols=" + StringMap.Keys.Join ( "," );
		}

		[DebuggerStepThrough]
		[Pure]
		public static FiatCurrency ToFiatCurrency ( this string symbol )
		{
			var upper = symbol.ToUpper ( );
			return StringMap.ContainsKey ( upper ) ? StringMap[upper] : FiatCurrency.USD;
		}

		public static Timer StartMonitor ( )
		{
			var timer = new Timer ( 60 * 60 * 100 );
			FetchRates ( );
			timer.Elapsed += ( sender,
			                   args ) => Task.Run ( ( ) =>
			{
				try
				{
					FetchRates ( );
				}
				catch ( Exception e )
				{
					Logger.Error ( e );
				}
			} );
			timer.Start ( );

			return timer;
		}

		public static void FetchRates ( )
		{
			try
			{
				var json = TickerUrl.GetStringAsync ( ).Result;
				var data = JsonConvert.DeserializeObject<dynamic> ( json );
				UsdTo =
					JsonConvert.DeserializeObject<Dictionary<FiatCurrency, decimal>> ( data.rates.ToString ( ) );
				var factor = UsdTo[FiatCurrency.USD];
				foreach ( var fiat in UsdTo.Keys.ToList ( ) )
					UsdTo[fiat] = Math.Round ( UsdTo[fiat] / factor, 2 );
				UsdTo[FiatCurrency.USD] = 1m;
				Console.WriteLine ( data.rates );
				Logger.Info ( "Fetched Fiat currency rates." );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
				throw;
			}
		}

		[DebuggerStepThrough]
		[Pure]
		public static decimal Convert ( decimal amount,
		                                FiatCurrency from,
		                                FiatCurrency to ) =>
			amount * UsdTo[to] / UsdTo[from];

		[DebuggerStepThrough]
		[Pure]
		public static string ToString ( decimal amount,
		                                FiatCurrency from,
		                                FiatCurrency to )
		{
			var result = Convert ( amount, from, to );
			var symbol = Map[to];

			return $"{symbol}{result:N}";
		}
	}
}