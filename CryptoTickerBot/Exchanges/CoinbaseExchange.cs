using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Helpers;
using Newtonsoft.Json;

namespace CryptoTickerBot.Exchanges
{
	public class CoinbaseExchange : CryptoExchangeBase
	{
		private static readonly List<string> Symbols = new List<string>
		{
			"BTC",
			"ETH",
			"BCH",
			"LTC"
		};

		public CoinbaseExchange ( )
		{
			Name = "Coinbase";
			Url = new Uri ( "https://www.coinbase.com/" );
			TickerUrl = new Uri ( "https://api.coinbase.com/v2/prices/" );
			Id = CryptoExchange.Coinbase;
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<string, CryptoCoin> ( );
			while ( !ct.IsCancellationRequested )
			{
				foreach ( var symbol in Symbols )
				{
					var tickerBuy = $"{TickerUrl}{symbol}-USD/buy";
					var tickerSell = $"{TickerUrl}{symbol}-USD/sell";
					var tickerSpot = $"{TickerUrl}{symbol}-USD/spot";

					var buy = JsonConvert.DeserializeObject<dynamic> ( await WebRequests.GetAsync ( tickerBuy ) );
					var sell = JsonConvert.DeserializeObject<dynamic> ( await WebRequests.GetAsync ( tickerSell ) );
					var spot = JsonConvert.DeserializeObject<dynamic> ( await WebRequests.GetAsync ( tickerSpot ) );

					dynamic data = new System.Dynamic.ExpandoObject ( );
					data.buy = buy;
					data.sell = sell;
					data.spot = spot;

					Update ( data, symbol );
				}

				await Task.Delay ( 1000, ct );
			}
		}

		protected override void Update ( dynamic data, string symbol )
		{
			if ( !ExchangeData.ContainsKey ( symbol ) )
				ExchangeData[symbol] = new CryptoCoin ( symbol );

			var old = ExchangeData[symbol].Clone ( );

			ExchangeData[symbol].LowestAsk = data.buy.data.amount;
			ExchangeData[symbol].HighestBid = data.sell.data.amount;
			ExchangeData[symbol].Rate = data.spot.data.amount;

			if ( old != ExchangeData[symbol] )
				OnChanged ( this, old );
		}
	}
}