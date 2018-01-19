using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Extensions;
using Newtonsoft.Json;
using WebSocketSharp;

namespace CryptoTickerBot.Exchanges
{
	public class CoinbaseExchange : CryptoExchangeBase
	{
		public CoinbaseExchange ( )
		{
			Name = "Coinbase";
			Url = "https://www.coinbase.com/";
			TickerUrl = "wss://ws-feed.gdax.com/";
			Id = CryptoExchange.Coinbase;
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<string, CryptoCoin> ( );

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

			LastUpdate = DateTime.Now;
		}

		protected override void Update ( dynamic data, string symbol )
		{
			if ( !ExchangeData.ContainsKey ( symbol ) )
				ExchangeData[symbol] = new CryptoCoin ( symbol );

			var old = ExchangeData[symbol].Clone ( );

			ExchangeData[symbol].LowestAsk = data.best_ask;
			ExchangeData[symbol].HighestBid = data.best_bid;
			ExchangeData[symbol].Rate = data.price;

			if ( old != ExchangeData[symbol] )
				OnChanged ( this, old );
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