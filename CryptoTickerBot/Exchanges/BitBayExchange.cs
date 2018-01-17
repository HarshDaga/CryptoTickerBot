using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Helpers;
using Newtonsoft.Json;

namespace CryptoTickerBot.Exchanges
{
	public class BitBayExchange : CryptoExchangeBase
	{
		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<string, CryptoCoin> ( );
			var tickers = new List<(string symbol, string url)>
			{
				("BTC", "https://bitbay.net/API/Public/BTC/ticker.json"),
				("ETH", "https://bitbay.net/API/Public/ETH/ticker.json"),
				("LTC", "https://bitbay.net/API/Public/LTC/ticker.json"),
				("BCH", "https://bitbay.net/API/Public/BCC/ticker.json")
			};

			while ( !ct.IsCancellationRequested )
			{
				foreach ( var ticker in tickers )
				{
					var symbol = ticker.symbol;
					var json = await WebRequests.GetAsync ( ticker.url );
					var data = JsonConvert.DeserializeObject<dynamic> ( json );

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

			ExchangeData[symbol].LowestAsk = data.ask;
			ExchangeData[symbol].HighestBid = data.bid;
			ExchangeData[symbol].Rate = data.last;

			if ( old != ExchangeData[symbol] )
				OnChanged ( this, old );
		}

		public BitBayExchange ( )
		{
			Name = "BitBay";
			Url = new Uri ( "https://bitbay.net/en" );
			TickerUrl = new Uri ( "https://api.bitbay.net/rest/trading/ticker" );
		}
	}
}