using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Exchanges.Core;
using Newtonsoft.Json;
using NLog;
using WebSocketSharp;
using Logger = NLog.Logger;

// ReSharper disable AccessToDisposedClosure

namespace CryptoTickerBot.Exchanges
{
	public class BinanceExchange : CryptoExchangeBase
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public BinanceExchange ( ) : base ( CryptoExchangeId.Binance )
		{
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<CryptoCoinId, CryptoCoin> ( );

			using ( var ws = new WebSocket ( TickerUrl ) )
			{
				await Task.Run ( ( ) => ws.Connect ( ), ct ).ConfigureAwait ( false );

				ws.OnMessage += WsOnMessage;

				while ( ws.IsAlive )
					await Task.Delay ( 100, ct ).ConfigureAwait ( false );
			}
		}

		private void WsOnMessage ( object sender, MessageEventArgs args )
		{
			try
			{
				var json = args.Data;
				var data = JsonConvert.DeserializeObject<dynamic> ( json );

				foreach ( var datum in data )
				{
					var s = (string) datum.s;
					var symbol = s.Substring ( 0, 3 );
					if ( !s.EndsWith ( "USDT" ) ) continue;
					if ( symbol == "BCC" ) symbol = "BCH";
					if ( !KnownSymbols.Contains ( symbol ) ) continue;
					Update ( datum, symbol );
				}
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		protected override void DeserializeData ( dynamic datum, CryptoCoinId id )
		{
			ExchangeData[id].LowestAsk  = datum.a;
			ExchangeData[id].HighestBid = datum.b;
			ExchangeData[id].Rate       = datum.w;
		}
	}
}