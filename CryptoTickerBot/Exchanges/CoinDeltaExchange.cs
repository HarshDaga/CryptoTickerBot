using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Helpers;
using Flurl.Http;
using Newtonsoft.Json;

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

			WithdrawalFees = new Dictionary<string, decimal>
			{
				["BTC"] = 0.001m,
				["ETH"] = 0.001m,
				["LTC"] = 0.002m,
				["BCH"] = 0.001m
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

		private class CoinDeltaCoin
		{
			[JsonProperty ( "Ask" )]
			public decimal Ask { get; set; }

			[JsonProperty ( "Bid" )]
			public decimal Bid { get; set; }

			[JsonProperty ( "MarketName" )]
			public string MarketName { get; set; }

			[JsonProperty ( "Last" )]
			public decimal Last { get; set; }
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<string, CryptoCoin> ( );

			while ( !ct.IsCancellationRequested )
			{
				var data = await TickerUrl.GetJsonAsync<List<CoinDeltaCoin>> ( ct );

				foreach ( var datum in data )
				{
					var marketname = datum.MarketName.Split ( '-' );
					if ( marketname[1] == "inr" && KnownSymbols.Contains ( marketname[0].ToUpper ( ) ) )
						Update ( datum, marketname[0].ToUpper ( ) );
				}

				await Task.Delay ( 60000, ct );
			}
		}

		protected override void DeserializeData ( dynamic data, string symbol )
		{
			var cdc = (CoinDeltaCoin) data;
			decimal InrToUsd ( decimal amount ) => FiatConverter.Convert ( amount, FiatCurrency.INR, FiatCurrency.USD );

			ExchangeData[symbol].LowestAsk = InrToUsd ( cdc.Ask );
			ExchangeData[symbol].HighestBid = InrToUsd ( cdc.Bid );
			ExchangeData[symbol].Rate = InrToUsd ( cdc.Last );
		}
	}
}