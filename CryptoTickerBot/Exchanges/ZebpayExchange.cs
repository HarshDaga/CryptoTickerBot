using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Exchanges.Core;
using CryptoTickerBot.Helpers;
using Flurl.Http;

namespace CryptoTickerBot.Exchanges
{
	public class ZebpayExchange : CryptoExchangeBase
	{
		public ZebpayExchange ( ) : base ( CryptoExchangeId.Zebpay )
		{
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<CryptoCoinId, CryptoCoin> ( );

			var tickers = new List<(string symbol, string url)>
			{
				( "BTC", "https://www.zebapi.com/api/v1/market/ticker-new/btc/inr" ),
				( "ETH", "https://www.zebapi.com/api/v1/market/ticker-new/eth/inr" ),
				( "LTC", "https://www.zebapi.com/api/v1/market/ticker-new/ltc/inr" ),
				( "BCH", "https://www.zebapi.com/api/v1/market/ticker-new/bch/inr" )
			};

			while ( !ct.IsCancellationRequested )
				foreach ( var ticker in tickers )
				{
					var symbol = ticker.symbol;
					try
					{
						var data = await ticker.url.GetJsonAsync ( ct ).ConfigureAwait ( false );

						Update ( data, symbol );
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
			decimal InrToUsd ( dynamic amount ) =>
				FiatConverter.Convert ( (decimal) amount, FiatCurrency.INR, FiatCurrency.USD );

			ExchangeData[id].LowestAsk  = InrToUsd ( data.buy );
			ExchangeData[id].HighestBid = InrToUsd ( data.sell );
			ExchangeData[id].Rate       = InrToUsd ( data.market );
		}
	}
}