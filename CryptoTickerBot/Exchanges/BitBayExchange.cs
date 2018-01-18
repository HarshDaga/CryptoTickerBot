using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;

namespace CryptoTickerBot.Exchanges
{
	public class BitBayExchange : CryptoExchangeBase
	{
		public BitBayExchange ( )
		{
			Name = "BitBay";
			Url = "https://bitbay.net/en";
			TickerUrl = "https://api.bitbay.net/rest/trading/ticker";
			Id = CryptoExchange.BitBay;
		}

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

			try
			{
				while ( !ct.IsCancellationRequested )
				{
					foreach ( var ticker in tickers )
					{
						var symbol = ticker.symbol;
						var data = await ticker.url.GetJsonAsync ( ct );

						Update ( data, symbol );

						LastUpdate = DateTime.Now;

						await Task.Delay ( 1000, ct );
					}
				}
			}
			catch ( Exception e )
			{
				Console.WriteLine ( e );
			}
		}

		protected override void Update ( dynamic data, string symbol )
		{
			if ( !ExchangeData.ContainsKey ( symbol ) )
				ExchangeData[symbol] = new CryptoCoin ( symbol );

			var old = ExchangeData[symbol].Clone ( );

			ExchangeData[symbol].LowestAsk = (decimal) data.ask;
			ExchangeData[symbol].HighestBid = (decimal) data.bid;
			ExchangeData[symbol].Rate = (decimal) data.last;

			if ( old != ExchangeData[symbol] )
				OnChanged ( this, old );
		}
	}
}