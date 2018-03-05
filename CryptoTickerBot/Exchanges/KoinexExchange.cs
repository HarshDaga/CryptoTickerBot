using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Exchanges.Core;
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
			["bitcoin"]      = "BTC",
			["bitcoin_cash"] = "BCH",
			["ether"]        = "ETH",
			["litecoin"]     = "LTC"
		};

		public KoinexExchange ( ) : base ( CryptoExchangeId.Koinex )
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

		protected override void DeserializeData ( dynamic data, CryptoCoinId id )
		{
			decimal InrToUsd ( decimal amount ) => FiatConverter.Convert ( amount, FiatCurrency.INR, FiatCurrency.USD );

			ExchangeData[id].LowestAsk  = InrToUsd ( data.lowest_ask );
			ExchangeData[id].HighestBid = InrToUsd ( data.highest_bid );
			ExchangeData[id].Rate       = InrToUsd ( data.last_traded_price );
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