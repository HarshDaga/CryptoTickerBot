using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Helpers;
using Newtonsoft.Json;

namespace CryptoTickerBot.Exchanges
{
	public class CoinDeltaExchange : CryptoExchangeBase
	{
		public CoinDeltaExchange ( )
		{
			Name = "CoinDelta";
			Url = new Uri ( "https://coindelta.com/" );
			TickerUrl = new Uri ( "https://coindelta.com/api/v1/public/getticker/" );
			Id = CryptoExchange.CoinDelta;
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<string, CryptoCoin> ( );

			while ( !ct.IsCancellationRequested )
			{
				var json = await WebRequests.GetAsync ( TickerUrl );
				var data = JsonConvert.DeserializeObject<dynamic> ( json );

				foreach ( var datum in data )
				{
					var marketname = ( (string) datum.MarketName ).Split ( '-' );
					if ( marketname[1] == "inr" )
						Update ( datum, marketname[0].ToUpper ( ) );
				}
				await Task.Delay ( 1000, ct );
			}
		}

		protected override void Update ( dynamic data, string symbol )
		{
			if ( !( new[] {"BTC", "ETH", "LTC", "BCH"} ).Contains ( symbol ) )
				return;

			if ( !ExchangeData.ContainsKey ( symbol ) )
				ExchangeData[symbol] = new CryptoCoin ( symbol );

			var old = ExchangeData[symbol].Clone ( );

			decimal InrToUsd ( decimal amount ) => FiatConverter.Convert ( amount, FiatCurrency.INR, FiatCurrency.USD );

			ExchangeData[symbol].LowestAsk = InrToUsd ( data.Ask );
			ExchangeData[symbol].HighestBid = InrToUsd ( data.Bid );
			ExchangeData[symbol].Rate = InrToUsd ( data.Last );

			if ( old != ExchangeData[symbol] )
				OnChanged ( this, old );
		}
	}
}