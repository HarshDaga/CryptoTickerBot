using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace CryptoTickerBot.Exchanges
{
	public class BinanceExchange : ICryptoExchange
	{
		public string Name { get; }
		public Uri Url { get; }
		public Uri TickerUrl { get; }
		public Dictionary<string, CryptoCoin> ExchangeData { get; private set; }

		public async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<string, CryptoCoin> ( );

			try
			{
				using ( var ws = new WebSocket ( TickerUrl.ToString ( ) ) )
				{
					ws.ConnectAsync ( );
					while ( ws.ReadyState == WebSocketState.Connecting )
						await Task.Delay ( 1, ct );

					ws.OnMessage += ( sender, args ) =>
					{
						var json = args.Data;
						var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic> ( json );

						foreach ( var datum in data )
						{
							var s = (string) datum.s;
							var symbol = s.Substring ( 0, 3 );
							if ( !s.EndsWith ( "USDT" ) )
								continue;
							Update ( datum, symbol );
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

		private void Update ( dynamic datum, string symbol )
		{
			if ( symbol == "BCC" )
				symbol = "BCH";

			if ( !ExchangeData.ContainsKey ( symbol ) )
				ExchangeData[symbol] = new CryptoCoin ( symbol );

			var old = ExchangeData[symbol].Clone ( );

			ExchangeData[symbol].LowestAsk = datum.a;
			ExchangeData[symbol].HighestBid = datum.b;
			ExchangeData[symbol].Rate = datum.w;

			if ( old != ExchangeData[symbol] )
				OnChanged?.BeginInvoke ( this, old, null, null );
		}

		public event Action<ICryptoExchange, CryptoCoin> OnChanged;

		public BinanceExchange ( )
		{
			Name = "Binance";
			Url = new Uri ( "https://www.binance.com/" );
			TickerUrl = new Uri ( "wss://stream2.binance.com:9443/ws/!ticker@arr@3000ms" );
		}
	}
}