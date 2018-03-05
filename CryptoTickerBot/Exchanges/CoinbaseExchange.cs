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
			ExchangeData = new Dictionary<CryptoCoinId, CryptoCoin> ( );

			using ( var ws = new WebSocket ( TickerUrl ) )
			{
				await ConnectAndSubscribe ( ws, ct );

				ws.OnMessage += Ws_OnMessage;

				await Task.Delay ( int.MaxValue, ct );
			}
		}

		private void Ws_OnMessage ( object sender, MessageEventArgs e )
		{
			var json = e.Data;
			var data = JsonConvert.DeserializeObject<dynamic> ( json );
			if ( data.type != "ticker" )
				return;

			var symbol = ( (string) data.product_id ).Substring ( 0, 3 );

			Update ( data, symbol );
		}

		protected override void DeserializeData ( dynamic data, CryptoCoinId id )
		{
			ExchangeData[id].LowestAsk  = data.best_ask;
			ExchangeData[id].HighestBid = data.best_bid;
			ExchangeData[id].Rate       = data.price;
		}

		private static async Task ConnectAndSubscribe ( WebSocket ws, CancellationToken ct )
		{
			await Task.Run ( ( ) => ws.Connect ( ), ct );

			var productIds = string.Join ( ",", KnownSymbols.Select ( x => $"\"{x}-USD\"" ) );
			await ws.SendStringAsync (
				$"{{\"type\":\"subscribe\",\"product_ids\":[{productIds}],\"channels\":[\"ticker\"]}}" );
		}
	}
}