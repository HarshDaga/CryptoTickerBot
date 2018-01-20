using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

		public BinanceExchange ( )
		{
			Name = "Binance";
			Url = "https://www.binance.com/";
			TickerUrl = "wss://stream2.binance.com:9443/ws/!ticker@arr@3000ms";
			Id = CryptoExchange.Binance;

			WithdrawalFees = new Dictionary<string, decimal>
			{
				["BTC"] = 0.001m,
				["ETH"] = 0.01m,
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

			BuyFees = 0.1m;
			SellFees = 0.1m;
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<string, CryptoCoin> ( );

			using ( var ws = new WebSocket ( TickerUrl ) )
			{
				await Task.Run ( ( ) => ws.Connect ( ), ct );

				ws.OnMessage += ( sender, args ) =>
				{
					try
					{
						var json = args.Data;
						var data = JsonConvert.DeserializeObject<dynamic> ( json );

						foreach ( var datum in data )
						{
							var s = (string) datum.s;
							var symbol = s.Substring ( 0, 3 );
							if ( !s.EndsWith ( "USDT" ) )
								continue;
							if ( symbol == "BCC" )
								symbol = "BCH";
							if ( !KnownSymbols.Contains ( symbol ) )
								continue;
							Update ( datum, symbol );

							LastUpdate = DateTime.Now;
						}
					}
					catch ( Exception e )
					{
						Logger.Error ( e );
					}
				};

				await Task.Delay ( int.MaxValue, ct );
			}
		}

		protected override void DeserializeData ( dynamic datum, string symbol )
		{
			ExchangeData[symbol].LowestAsk = datum.a;
			ExchangeData[symbol].HighestBid = datum.b;
			ExchangeData[symbol].Rate = datum.w;
		}
	}
}