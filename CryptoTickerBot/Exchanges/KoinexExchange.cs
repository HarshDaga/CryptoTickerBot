using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Extensions;
using CryptoTickerBot.Helpers;
using WebSocketSharp;
using static Newtonsoft.Json.JsonConvert;

// ReSharper disable StringIndexOfIsCultureSpecific.1

namespace CryptoTickerBot.Exchanges
{
	public class KoinexExchange : CryptoExchangeBase
	{
		private static readonly Dictionary<string, string> ToSymBol = new Dictionary<string, string>
		{
			["bitcoin"] = "BTC",
			["bitcoin_cash"] = "BCH",
			["ether"] = "ETH",
			["litecoin"] = "LTC",
		};

		public KoinexExchange ( )
		{
			Name = "Koinex";
			Url = "https://koinex.in/";
			TickerUrl = "wss://ws-ap2.pusher.com/app/9197b0bfdf3f71a4064e?protocol=7&client=js&version=4.1.0&flash=false";
			Id = CryptoExchange.Koinex;
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
			var data = DeserializeObject<dynamic> ( json );

			var eventName = (string) data.@event;
			if ( !eventName.EndsWith ( "_market_data" ) )
				return;

			var prefix = eventName.Substring ( 0, eventName.IndexOf ( "_market_data" ) );
			if ( !ToSymBol.ContainsKey ( prefix ) )
				return;
			var symbol = ToSymBol[prefix];

			Update ( DeserializeObject<dynamic> ( (string) data.data ).message.data, symbol );
		}

		protected override void Update ( dynamic data, string symbol )
		{
			if ( !ExchangeData.ContainsKey ( symbol ) )
				ExchangeData[symbol] = new CryptoCoin ( symbol );

			var old = ExchangeData[symbol].Clone ( );

			decimal InrToUsd ( decimal amount ) => FiatConverter.Convert ( amount, FiatCurrency.INR, FiatCurrency.USD );

			ExchangeData[symbol].LowestAsk = InrToUsd ( data.lowest_ask );
			ExchangeData[symbol].HighestBid = InrToUsd ( data.highest_bid );
			ExchangeData[symbol].Rate = InrToUsd ( data.last_traded_price );

			if ( old != ExchangeData[symbol] )
				OnChanged ( this, old );
		}

		public static async Task ConnectAndSubscribe ( WebSocket ws, CancellationToken ct )
		{
			ws.ConnectAsync ( );
			while ( ws.ReadyState == WebSocketState.Connecting )
				await Task.Delay ( 1, ct );

			foreach ( var name in ToSymBol.Keys )
				await ws.SendStringAsync ( $"{{\"event\":\"pusher:subscribe\",\"data\":{{\"channel\":\"my-channel-{name}\"}}}}" );
		}
	}
}