using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Exchanges.Core;
using Flurl.Http;

namespace CryptoTickerBot.Exchanges
{
	public class BitBayExchange : CryptoExchangeBase
	{
		public BitBayExchange ( ) : base ( CryptoExchangeId.BitBay )
		{
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new ConcurrentDictionary<CryptoCoinId, CryptoCoin> ( );

			var tickers = new List<(string symbol, string url)>
			{
				( "BTC", "https://bitbay.net/API/Public/BTC/ticker.json" ),
				( "ETH", "https://bitbay.net/API/Public/ETH/ticker.json" ),
				( "LTC", "https://bitbay.net/API/Public/LTC/ticker.json" ),
				( "BCH", "https://bitbay.net/API/Public/BCC/ticker.json" )
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
			ExchangeData[id].LowestAsk  = (decimal) data.ask;
			ExchangeData[id].HighestBid = (decimal) data.bid;
			ExchangeData[id].Rate       = (decimal) data.last;
		}
	}
}