using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Exchanges.Core;
using CryptoTickerBot.Helpers;
using Flurl.Http;
using Newtonsoft.Json;

namespace CryptoTickerBot.Exchanges
{
	public class CoinDeltaExchange : CryptoExchangeBase
	{
		public CoinDeltaExchange ( ) : base ( CryptoExchangeId.CoinDelta )
		{
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<CryptoCoinId, CryptoCoin> ( );

			while ( !ct.IsCancellationRequested )
			{
				var data = await TickerUrl.GetJsonAsync<List<CoinDeltaCoin>> ( ct ).ConfigureAwait ( false );

				foreach ( var datum in data )
				{
					var marketname = datum.MarketName.Split ( '-' );
					if ( marketname[1] == "inr" && KnownSymbols.Contains ( marketname[0].ToUpper ( ) ) )
						Update ( datum, marketname[0].ToUpper ( ) );
				}

				await Task.Delay ( 60000, ct ).ConfigureAwait ( false );
			}
		}

		protected override void DeserializeData ( dynamic data, CryptoCoinId id )
		{
			var cdc = (CoinDeltaCoin) data;
			decimal InrToUsd ( decimal amount ) => FiatConverter.Convert ( amount, FiatCurrency.INR, FiatCurrency.USD );

			ExchangeData[id].LowestAsk  = InrToUsd ( cdc.Ask );
			ExchangeData[id].HighestBid = InrToUsd ( cdc.Bid );
			ExchangeData[id].Rate       = InrToUsd ( cdc.Last );
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
	}
}