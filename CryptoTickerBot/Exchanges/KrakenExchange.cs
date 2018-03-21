using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Exchanges.Core;
using Flurl.Http;
using Newtonsoft.Json;
using NLog;

namespace CryptoTickerBot.Exchanges
{
	public class KrakenExchange : CryptoExchangeBase
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public KrakenExchange ( ) : base ( CryptoExchangeId.Kraken )
		{
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<CryptoCoinId, CryptoCoin> ( );

			while ( !ct.IsCancellationRequested )
			{
				try
				{
					var data = await TickerUrl.GetJsonAsync<Root> ( ct ).ConfigureAwait ( false );

					Update ( data.Result.Btc, "BTC" );
					Update ( data.Result.Bch, "BCH" );
					Update ( data.Result.Eth, "ETH" );
					Update ( data.Result.Ltc, "LTC" );
				}
				catch ( FlurlHttpException e )
				{
					if ( e.InnerException is TaskCanceledException )
						throw e.InnerException;
				}

				await Task.Delay ( 2000, ct ).ConfigureAwait ( false );
			}
		}

		protected override void DeserializeData ( dynamic data, CryptoCoinId id )
		{
			KrakenCoinInfo coinInfo = data;

			ExchangeData[id].LowestAsk  = coinInfo.Ask[0];
			ExchangeData[id].HighestBid = coinInfo.Bid[0];
			ExchangeData[id].Rate       = coinInfo.LastTrade[0];
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
	}
}