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
			Name      = "Coinbase";
			Url       = "https://www.coinbase.com/";
			TickerUrl = "wss://ws-feed.gdax.com/";
			Id        = CryptoExchange.Coinbase;

			WithdrawalFees = new Dictionary<string, decimal>
			{
				["BTC"] = 0.001m,
				["ETH"] = 0.003m,
				["LTC"] = 0.01m,
				["BCH"] = 0.001m
			};
			DepositFees = new Dictionary<string, decimal>
			{
				["BTC"] = 0m,
				["ETH"] = 0m,
				["LTC"] = 0m,
				["BCH"] = 0m
			};

			BuyFees  = 0.3m;
			SellFees = 0.3m;
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
		}

		protected override void DeserializeData ( dynamic data, string symbol )
		{
			ExchangeData[symbol].LowestAsk  = data.best_ask;
			ExchangeData[symbol].HighestBid = data.best_bid;
			ExchangeData[symbol].Rate       = data.price;
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