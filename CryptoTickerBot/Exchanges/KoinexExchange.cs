using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Exchanges.Core;
using CryptoTickerBot.Extensions;
using CryptoTickerBot.Helpers;
using Newtonsoft.Json;
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
			var data = DeserializeObject<KoinexTickerDatum> ( e.Data );

			if ( !data.Event.EndsWith ( "_market_data" ) )
				return;

			var prefix = data.Event.Substring ( 0, data.Event.IndexOf ( "_market_data" ) );
			if ( !ToSymBol.ContainsKey ( prefix ) )
				return;
			var symbol = ToSymBol[prefix];

			Update ( DeserializeObject<MessageWrapper> ( data.Data ).Message.Data, symbol );
		}

		protected override void DeserializeData ( dynamic data, CryptoCoinId id )
		{
			var tikerData = (TickerData) data;
			decimal InrToUsd ( decimal amount ) => FiatConverter.Convert ( amount, FiatCurrency.INR, FiatCurrency.USD );

			ExchangeData[id].LowestAsk  = InrToUsd ( tikerData.LowestAsk );
			ExchangeData[id].HighestBid = InrToUsd ( tikerData.HighestBid );
			ExchangeData[id].Rate       = InrToUsd ( tikerData.LastTradedPrice );
		}

		public static async Task ConnectAndSubscribe ( WebSocket ws, CancellationToken ct )
		{
			ws.ConnectAsync ( );
			while ( ws.ReadyState == WebSocketState.Connecting )
				await Task.Delay ( 1, ct ).ConfigureAwait ( false );

			foreach ( var name in ToSymBol.Keys )
				await ws.SendStringAsync ( $"{{\"event\":\"pusher:subscribe\",\"data\":{{\"channel\":\"my-channel-{name}\"}}}}" )
					.ConfigureAwait ( false );
		}

		private class KoinexTickerDatum
		{
			[JsonProperty ( "event" )]
			public string Event { get; set; }

			[JsonProperty ( "data" )]
			public string Data { get; set; }

			[JsonProperty ( "channel" )]
			public string Channel { get; set; }
		}

		private class MessageWrapper
		{
			[JsonProperty ( "message" )]
			public Message Message { get; set; }
		}

		private class Message
		{
			[JsonProperty ( "data" )]
			public TickerData Data { get; set; }
		}

		private class TickerData
		{
			[JsonProperty ( "last_traded_price" )]
			public decimal LastTradedPrice { get; set; }

			[JsonProperty ( "lowest_ask" )]
			public decimal LowestAsk { get; set; }

			[JsonProperty ( "highest_bid" )]
			public decimal HighestBid { get; set; }

			[JsonProperty ( "min" )]
			public decimal Min { get; set; }

			[JsonProperty ( "max" )]
			public decimal Max { get; set; }

			[JsonProperty ( "vol" )]
			public long Vol { get; set; }
		}
	}
}