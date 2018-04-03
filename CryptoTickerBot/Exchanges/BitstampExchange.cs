using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Extensions;
using CryptoTickerBot.Exchanges.Core;
using CryptoTickerBot.Extensions;
using WebSocketSharp;
using static Newtonsoft.Json.JsonConvert;

// ReSharper disable StringIndexOfIsCultureSpecific.1

namespace CryptoTickerBot.Exchanges
{
	public class BitstampExchange : CryptoExchangeBase
	{
		private static readonly Dictionary<string, string> ToSymBol = new Dictionary<string, string>
		{
			["live_trades"]        = "BTC",
			["live_trades_bchusd"] = "BCH",
			["live_trades_ethusd"] = "ETH",
			["live_trades_ltcusd"] = "LTC"
		};

		public BitstampExchange ( ) : base ( CryptoExchangeId.Bitstamp )
		{
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<CryptoCoinId, CryptoCoin> ( );

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
			var data = DeserializeObject<dynamic> ( e.Data );

			var eventName = (string) data.@event;
			if ( eventName != "trade" )
				return;

			var channel = (string) data.channel;
			if ( !ToSymBol.ContainsKey ( channel ) )
				return;
			var symbol = ToSymBol[channel];

			Update ( DeserializeObject<dynamic> ( (string) data.data ), symbol );
		}

		protected override void DeserializeData ( dynamic data, CryptoCoinId id )
		{
			if ( data.type == 1 )
				ExchangeData[id].HighestBid = data.price;
			else
				ExchangeData[id].LowestAsk = data.price;
			ExchangeData[id].Rate = data.price;
			ExchangeData[id].Time = DateTimeOffset
				.FromUnixTimeSeconds ( (long) data.timestamp )
				.UtcDateTime;
		}

		protected override void Update ( dynamic data, string symbol )
		{
			CryptoCoin old = null;
			var id = symbol.ToEnum ( CryptoCoinId.NULL );
			if ( ExchangeData.ContainsKey ( id ) )
				old = ExchangeData[id].Clone ( );
			else
				ExchangeData[id] = new CryptoCoin ( symbol );

			DeserializeData ( data, id );

			ApplyFees ( id );

			LastUpdate = DateTime.UtcNow;
			OnNext ( ExchangeData[id].Clone ( ) );

			if ( ExchangeData[id] != old )
				OnChanged ( ExchangeData[id] );
		}

		public static async Task ConnectAndSubscribe ( WebSocket ws, CancellationToken ct )
		{
			ws.ConnectAsync ( );
			while ( ws.ReadyState == WebSocketState.Connecting )
				await Task.Delay ( 1, ct ).ConfigureAwait ( false );

			foreach ( var channel in ToSymBol.Keys )
				await ws.SendStringAsync (
					$"{{\"event\":\"pusher:subscribe\",\"data\":{{\"channel\":\"{channel}\"}}}}"
				).ConfigureAwait ( false );
		}
	}
}