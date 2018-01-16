using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;

namespace CryptoTickerBot.Helpers
{
	public enum FiatCurrency
	{
		USD,
		AUD,
		BGN,
		BRL,
		CAD,
		CHF,
		CNY,
		CZK,
		DKK,
		EUR,
		GBP,
		HKD,
		HRK,
		HUF,
		IDR,
		ILS,
		INR,
		JPY,
		KRW,
		MXN,
		MYR,
		NOK,
		NZD,
		PHP,
		PLN,
		RON,
		RUB,
		SEK,
		SGD,
		THB,
		TRY,
		ZAR
	}

	public static class FiatConverter
	{
		public static Dictionary<FiatCurrency, decimal> UsdTo { get; private set; } =
			new Dictionary<FiatCurrency, decimal> ( );

		public static void StartMonitor ( )
		{
			var timer = new Timer ( 60 * 60 * 100 );
			FetchRates ( );
			timer.Elapsed += ( sender, args ) => Task.Run ( ( ) => FetchRates ( ) );
			timer.Start ( );
		}

		public static void FetchRates ( )
		{
			try
			{
				var json = WebRequests.Get ( "http://api.fixer.io/latest?base=USD" );
				var data = JsonConvert.DeserializeObject<dynamic> ( json );
				UsdTo = JsonConvert.DeserializeObject<Dictionary<FiatCurrency, decimal>> ( data.rates.ToString ( ) );
				UsdTo[FiatCurrency.USD] = 1m;
				Console.WriteLine ( data.rates );
			}
			catch ( Exception e )
			{
				Console.WriteLine ( e );
				throw;
			}
		}

		public static decimal Convert ( decimal amount, FiatCurrency from, FiatCurrency to ) =>
			amount * UsdTo[to] / UsdTo[from];
	}
}