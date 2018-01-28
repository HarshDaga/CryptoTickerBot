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

			WithdrawalFees = new Dictionary<string, decimal>
			{
				["BTC"] = 0.0009m,
				["ETH"] = 0.00126m,
				["LTC"] = 0.005m,
				["BCH"] = 0.0006m
			};
			DepositFees = new Dictionary<string, decimal>
			{
				["BTC"] = 0m,
				["ETH"] = 0m,
				["LTC"] = 0m,
				["BCH"] = 0m
			};

			BuyFees = 0.3m;
			SellFees = 0.3m;
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

			while ( !ct.IsCancellationRequested )
				foreach ( var ticker in tickers )
				{
					var symbol = ticker.symbol;
					var data = await ticker.url.GetJsonAsync ( ct );

					Update ( data, symbol );

					await Task.Delay ( 2000, ct );
				}
		}

		protected override void DeserializeData ( dynamic data, string symbol )
		{
			ExchangeData[symbol].LowestAsk = (decimal) data.ask;
			ExchangeData[symbol].HighestBid = (decimal) data.bid;
			ExchangeData[symbol].Rate = (decimal) data.last;
		}
	}
}