using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CryptoTickerBot.Core.Configs;
using CryptoTickerBot.Core.Extensions;
using Flurl.Http;
using Newtonsoft.Json;
using NLog;

namespace CryptoTickerBot.Core.Helpers
{
	public static class FiatConverter
	{
		public static readonly string TickerUrl = "http://data.fixer.io/api/latest";
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		private static readonly IDictionary<string, RegionInfo> Map;

		public static Dictionary<string, decimal> UsdTo { get; private set; } =
			new Dictionary<string, decimal> ( );

		public static bool IsRunning { get; private set; }
		public static Timer Timer { get; private set; }

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
					ri => ri != null )
				.GroupBy ( ri => ri.ISOCurrencySymbol )
				.OrderBy ( x => x.Key )
				.ToDictionary (
					x => x.Key,
					x => x.First ( )
				);

			TickerUrl += $"?access_key={ConfigManager<CoreConfig>.Instance.FixerApiKey}";
			TickerUrl += "&symbols=" + Map.Keys.Join ( "," );
		}

		public static async Task<Timer> StartMonitor ( )
		{
			if ( IsRunning )
				return Timer;
			IsRunning = true;

			Timer = new Timer ( TimeSpan.FromDays ( 1 ).TotalMilliseconds );
			Timer.Disposed += ( sender,
			                    args ) => IsRunning = false;
			await FetchRates ( );
			Timer.Elapsed += ( sender,
			                   args ) => Task.Run ( async ( ) => await FetchRates ( ) );
			Timer.Start ( );

			return Timer;
		}

		public static void StopMonitor ( )
		{
			Timer?.Stop ( );
			Timer?.Dispose ( );
		}

		public static async Task FetchRates ( )
		{
			try
			{
				var json = await TickerUrl.GetStringAsync ( ).ConfigureAwait ( false );
				var data = JsonConvert.DeserializeObject<dynamic> ( json );
				UsdTo =
					JsonConvert.DeserializeObject<Dictionary<string, decimal>> ( data.rates.ToString ( ) );

				var factor = UsdTo["USD"];
				var symbols = UsdTo.Keys.ToList ( );
				foreach ( var fiat in symbols )
					UsdTo[fiat] = Math.Round ( UsdTo[fiat] / factor, 2 );
				UsdTo["USD"] = 1m;

				Logger.Info ( $"Fetched Fiat currency rates for {UsdTo.Count} symbols." );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
				throw;
			}
		}

		public static Dictionary<string, RegionInfo> GetSymbols ( ) =>
			new Dictionary<string, RegionInfo> ( Map );

		[DebuggerStepThrough]
		[Pure]
		public static decimal Convert ( decimal amount,
		                                string from,
		                                string to ) =>
			amount * UsdTo[to] / UsdTo[from];

		[DebuggerStepThrough]
		[Pure]
		public static string ToString ( decimal amount,
		                                string from,
		                                string to )
		{
			var result = Convert ( amount, from, to );
			var symbol = Map[to];

			return $"{symbol.ISOCurrencySymbol}{result:N}";
		}
	}
}