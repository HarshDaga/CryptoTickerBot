using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Newtonsoft.Json;

namespace CryptoTickerBot.Exchanges
{
	public class KrakenExchange : CryptoExchangeBase
	{
		public KrakenExchange ( )
		{
			Name = "Kraken";
			Url = "https://www.kraken.com/";
			TickerUrl = "https://api.kraken.com/0/public/Ticker?pair=XBTUSD,BCHUSD,ETHUSD,LTCUSD";
			Id = CryptoExchange.Kraken;

			WithdrawalFees = new Dictionary<string, decimal>
			{
				["BTC"] = 0.00025m,
				["ETH"] = 0.005m,
				["LTC"] = 0.02m,
				["BCH"] = 0.001m
			};
			DepositFees = new Dictionary<string, decimal>
			{
				["BTC"] = 0m,
				["ETH"] = 0m,
				["LTC"] = 0m,
				["BCH"] = 0m
			};

			BuyFees = 0.26m;
			SellFees = 0.26m;
		}

		#region JSON Structure

		private class Root
		{
			[JsonProperty ( "error" )]
			public List<object> Error { get; set; }

			[JsonProperty ( "result" )]
			public Result Result { get; set; }
		}

		private class Result
		{
			[JsonProperty ( "BCHUSD" )]
			public KrakenCoinInfo Bch { get; set; }

			[JsonProperty ( "XETHZUSD" )]
			public KrakenCoinInfo Eth { get; set; }

			[JsonProperty ( "XLTCZUSD" )]
			public KrakenCoinInfo Ltc { get; set; }

			[JsonProperty ( "XXBTZUSD" )]
			public KrakenCoinInfo Btc { get; set; }
		}

		private class KrakenCoinInfo
		{
			[JsonProperty ( "a" )]
			public List<decimal> Ask { get; set; }

			[JsonProperty ( "b" )]
			public List<decimal> Bid { get; set; }

			[JsonProperty ( "c" )]
			public List<decimal> LastTrade { get; set; }

			[JsonProperty ( "v" )]
			public List<decimal> Volume { get; set; }

			[JsonProperty ( "p" )]
			public List<decimal> Price { get; set; }

			[JsonProperty ( "t" )]
			public List<decimal> Trades { get; set; }

			[JsonProperty ( "l" )]
			public List<decimal> Low { get; set; }

			[JsonProperty ( "h" )]
			public List<decimal> High { get; set; }

			[JsonProperty ( "o" )]
			public decimal Open { get; set; }
		}

		#endregion

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<string, CryptoCoin> ( );
			Observables = new ConcurrentDictionary<string, ImmutableHashSet<IObserver<CryptoCoin>>> ( );

			while ( !ct.IsCancellationRequested )
			{
				var data = await TickerUrl.GetJsonAsync<Root> ( ct );

				Update ( data.Result.Btc, "BTC" );
				Update ( data.Result.Bch, "BCH" );
				Update ( data.Result.Eth, "ETH" );
				Update ( data.Result.Ltc, "LTC" );

				LastUpdate = DateTime.Now;

				await Task.Delay ( 2000, ct );
			}
		}

		protected override void DeserializeData ( dynamic data, string symbol )
		{
			KrakenCoinInfo coinInfo = data;

			ExchangeData[symbol].LowestAsk = coinInfo.Ask[0];
			ExchangeData[symbol].HighestBid = coinInfo.Bid[0];
			ExchangeData[symbol].Rate = coinInfo.LastTrade[0];
		}
	}
}