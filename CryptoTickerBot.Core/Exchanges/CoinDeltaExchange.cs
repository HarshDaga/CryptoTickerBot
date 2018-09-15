using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Data.Domain;
using Flurl.Http;
using Newtonsoft.Json;

namespace CryptoTickerBot.Core.Exchanges
{
	public class CoinDeltaExchange : CryptoExchangeBase<CoinDeltaExchange.CoinDeltaCoin>
	{
		public CoinDeltaExchange ( ) : base ( CryptoExchangeId.CoinDelta )
		{
		}

		protected override async Task GetExchangeData ( CancellationToken ct )
		{
			while ( !ct.IsCancellationRequested )
			{
				var data = await TickerUrl.GetJsonAsync<List<CoinDeltaCoin>> ( ct ).ConfigureAwait ( false );

				foreach ( var datum in data )
				{
					var market = datum.MarketName.Replace ( "-", "" );
					Update ( datum, market.ToUpper ( ) );
				}

				await Task.Delay ( PollingRate, ct ).ConfigureAwait ( false );
			}
		}

		protected override void DeserializeData ( CoinDeltaCoin data,
		                                          string id )
		{
			ExchangeData[id].LowestAsk  = data.Ask;
			ExchangeData[id].HighestBid = data.Bid;
			ExchangeData[id].Rate       = data.Last;
		}

		public class CoinDeltaCoin
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
	}
}