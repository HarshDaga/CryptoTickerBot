using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Extensions;
using CryptoTickerBot.Helpers;
using Newtonsoft.Json;
using WebSocketSharp;

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

			try
			{
				using ( var ws = new WebSocket ( TickerUrl ) )
				{
					await ConnectAndSubscribe ( ws, ct );

					ws.OnMessage += Ws_OnMessage;

					await Task.Delay ( int.MaxValue, ct );
				}
			}
			catch ( Exception e )
			{
				Console.WriteLine ( e );
				throw;
			}
		}

		private void Ws_OnMessage ( object sender, MessageEventArgs e )
		{
			var json = e.Data;
			var data = JsonConvert.DeserializeObject<dynamic> ( json );

			var eventName = (string) data.@event;
			if ( !eventName.EndsWith ( "_market_data" ) )
				return;

			var prefix = eventName.Substring ( 0, eventName.IndexOf ( "_market_data" ) );
			if ( !ToSymBol.ContainsKey ( prefix ) )
				return;
			var symbol = ToSymBol[prefix];

			Update ( data, symbol );
		}

		protected override void Update ( dynamic data, string symbol )
		{
			if ( !ExchangeData.ContainsKey ( symbol ) )
				ExchangeData[symbol] = new CryptoCoin ( symbol );

			var info = JsonConvert.DeserializeObject<dynamic> ( (string) data.data );
			var old = ExchangeData[symbol].Clone ( );

			decimal InrToUsd ( decimal amount ) => FiatConverter.Convert ( amount, FiatCurrency.INR, FiatCurrency.USD );

			ExchangeData[symbol].LowestAsk = InrToUsd ( info.message.data.lowest_ask );
			ExchangeData[symbol].HighestBid = InrToUsd ( info.message.data.highest_bid );
			ExchangeData[symbol].Rate = InrToUsd ( info.message.data.last_traded_price );

			if ( old != ExchangeData[symbol] )
				OnChanged ( this, old );
		}

		private static async Task ConnectAndSubscribe ( WebSocket ws, CancellationToken ct )
		{
			await Task.Run ( ( ) => ws.Connect ( ), ct );

			foreach ( var channel in ToSymBol.Keys )
				await ws.SendStringAsync (
					$"{{\"event\":\"pusher:subscribe\",\"data\":{{\"channel\":\"my-channel-{channel}\"}}}}" );
		}
	}
}