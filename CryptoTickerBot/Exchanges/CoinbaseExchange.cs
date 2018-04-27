using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Exchanges.Core;
using CryptoTickerBot.Extensions;
using Newtonsoft.Json;
using WebSocketSharp;

namespace CryptoTickerBot.Exchanges
{
	public class CoinbaseExchange : CryptoExchangeBase
	{
		public CoinbaseExchange ( ) : base ( CryptoExchangeId.Coinbase )
		{
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new ConcurrentDictionary<CryptoCoinId, CryptoCoin> ( );

			using ( var ws = new WebSocket ( TickerUrl ) )
			{
				await ConnectAndSubscribe ( ws, ct ).ConfigureAwait ( false );

				ws.OnMessage += Ws_OnMessage;

				while ( ws.Ping ( ) )
					await Task.Delay ( 60000, ct ).ConfigureAwait ( false );
			}
		}

		private void Ws_OnMessage ( object sender, MessageEventArgs e )
		{
			var data = JsonConvert.DeserializeObject<CoinbaseTickerDatum> ( e.Data );
			if ( data.Type != "ticker" )
				return;

			var symbol = data.ProductId.Substring ( 0, 3 );

			Update ( data, symbol );
		}

		protected override void DeserializeData ( dynamic data, CryptoCoinId id )
		{
			var tickerData = (CoinbaseTickerDatum) data;

			ExchangeData[id].LowestAsk  = tickerData.BestAsk;
			ExchangeData[id].HighestBid = tickerData.BestBid;
			ExchangeData[id].Rate       = tickerData.Price;
		}

		private static async Task ConnectAndSubscribe ( WebSocket ws, CancellationToken ct )
		{
			await Task.Run ( ( ) => ws.Connect ( ), ct ).ConfigureAwait ( false );

			var productIds = string.Join ( ",", KnownSymbols.Select ( x => $"\"{x}-USD\"" ) );
			await ws.SendStringAsync (
					$"{{\"type\":\"subscribe\",\"product_ids\":[{productIds}],\"channels\":[\"ticker\"]}}" )
				.ConfigureAwait ( false );
		}

		private class CoinbaseTickerDatum
		{
			[JsonProperty ( "type" )]
			public string Type { get; set; }

			[JsonProperty ( "sequence" )]
			public long Sequence { get; set; }

			[JsonProperty ( "product_id" )]
			public string ProductId { get; set; }

			[JsonProperty ( "price" )]
			public decimal Price { get; set; }

			[JsonProperty ( "open_24h" )]
			public decimal Open24H { get; set; }

			[JsonProperty ( "volume_24h" )]
			public decimal Volume24H { get; set; }

			[JsonProperty ( "low_24h" )]
			public decimal Low24H { get; set; }

			[JsonProperty ( "high_24h" )]
			public decimal High24H { get; set; }

			[JsonProperty ( "volume_30d" )]
			public decimal Volume30D { get; set; }

			[JsonProperty ( "best_bid" )]
			public decimal BestBid { get; set; }

			[JsonProperty ( "best_ask" )]
			public decimal BestAsk { get; set; }
		}
	}
}