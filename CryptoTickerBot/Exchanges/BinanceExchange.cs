using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebSocketSharp;

namespace CryptoTickerBot.Exchanges
{
	public class BinanceExchange : CryptoExchangeBase
	{
		public BinanceExchange ( )
		{
			Name = "Binance";
			Url = "https://www.binance.com/";
			TickerUrl = "wss://stream2.binance.com:9443/ws/!ticker@arr@3000ms";
			Id = CryptoExchange.Binance;
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<string, CryptoCoin> ( );

			try
			{
				using ( var ws = new WebSocket ( TickerUrl ) )
				{
					ws.ConnectAsync ( );
					while ( ws.ReadyState == WebSocketState.Connecting )
						await Task.Delay ( 1, ct );

					ws.OnMessage += ( sender, args ) =>
					{
						var json = args.Data;
						var data = JsonConvert.DeserializeObject<dynamic> ( json );

						foreach ( var datum in data )
						{
							var s = (string) datum.s;
							var symbol = s.Substring ( 0, 3 );
							if ( !s.EndsWith ( "USDT" ) )
								continue;
							Update ( datum, symbol );

							LastUpdate = DateTime.Now;
						}
					};

					await Task.Delay ( int.MaxValue, ct );
				}
			}
			catch ( Exception e )
			{
				Console.WriteLine ( e );
				throw;
			}
		}

		protected override void Update ( dynamic datum, string symbol )
		{
			if ( symbol == "BCC" )
				symbol = "BCH";
			if ( !KnownSymbols.Contains ( symbol ) )
				return;

			if ( !ExchangeData.ContainsKey ( symbol ) )
				ExchangeData[symbol] = new CryptoCoin ( symbol );

			var old = ExchangeData[symbol].Clone ( );

			ExchangeData[symbol].LowestAsk = datum.a;
			ExchangeData[symbol].HighestBid = datum.b;
			ExchangeData[symbol].Rate = datum.w;

			if ( old != ExchangeData[symbol] )
				OnChanged ( this, old );
		}
	}
}