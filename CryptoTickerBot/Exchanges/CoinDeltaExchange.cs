using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Helpers;
using Flurl.Http;

namespace CryptoTickerBot.Exchanges
{
	public class CoinDeltaExchange : CryptoExchangeBase
	{
		public CoinDeltaExchange ( )
		{
			Name = "CoinDelta";
			Url = "https://coindelta.com/";
			TickerUrl = "https://coindelta.com/api/v1/public/getticker/";
			Id = CryptoExchange.CoinDelta;
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<string, CryptoCoin> ( );

			while ( !ct.IsCancellationRequested )
			{
				//var json = await WebRequests.GetAsync ( TickerUrl );
				//var data = JsonConvert.DeserializeObject<dynamic> ( json );
				var data = await TickerUrl.GetJsonAsync ( ct );

				foreach ( var datum in data )
				{
					var marketname = ( (string) datum.MarketName ).Split ( '-' );
					if ( marketname[1] == "inr" )
						Update ( datum, marketname[0].ToUpper ( ) );
				}

				await Task.Delay ( 1000, ct );
			}
		}

		protected override void DeserializeData ( dynamic data, string symbol )
		{
			decimal InrToUsd ( decimal amount ) => FiatConverter.Convert ( amount, FiatCurrency.INR, FiatCurrency.USD );

			ExchangeData[symbol].LowestAsk = InrToUsd ( data.Ask );
			ExchangeData[symbol].HighestBid = InrToUsd ( data.Bid );
			ExchangeData[symbol].Rate = InrToUsd ( data.Last );
		}
	}
}