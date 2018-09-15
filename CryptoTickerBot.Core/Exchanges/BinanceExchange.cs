using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Data.Domain;
using Newtonsoft.Json;
using NLog;
using PureWebSockets;

namespace CryptoTickerBot.Core.Exchanges
{
	public class BinanceExchange : CryptoExchangeBase<BinanceExchange.BinanceTickerDatum>
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public BinanceExchange ( ) : base ( CryptoExchangeId.Binance )
		{
			TickerUrl = $"{TickerUrl}@{PollingRate.TotalMilliseconds}ms";
		}

		protected override async Task GetExchangeData ( CancellationToken ct )
		{
			var options = new PureWebSocketOptions
			{
				DebugMode = false
			};

			using ( var ws = new PureWebSocket ( TickerUrl, options ) )
			{
				ws.OnMessage += WsOnMessage;
				if ( !ws.Connect ( ) )
					Logger.Error ( "Couldn't connect to Binance" );

				while ( ws.State != WebSocketState.Closed )
					await Task.Delay ( PollingRate, ct ).ConfigureAwait ( false );
			}
		}

		protected override void DeserializeData ( BinanceTickerDatum datum,
		                                          string id )
		{
			ExchangeData[id].LowestAsk  = datum.BestAskPrice;
			ExchangeData[id].HighestBid = datum.BestBidPrice;
			ExchangeData[id].Rate       = datum.Close;
		}

		private void WsOnMessage ( string json )
		{
			try
			{
				var data = JsonConvert.DeserializeObject<List<BinanceTickerDatum>> ( json );

				foreach ( var datum in data )
					Update ( datum, datum.Symbol );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		public class BinanceTickerDatum
		{
			[JsonProperty ( "e" )]
			public string EventType { get; set; }

			[JsonProperty ( "E" )]
			public long Time { get; set; }

			[JsonProperty ( "s" )]
			public string Symbol { get; set; }

			[JsonProperty ( "p" )]
			public decimal PriceChange { get; set; }

			[JsonProperty ( "P" )]
			public decimal PriceChangePercent { get; set; }

			[JsonProperty ( "w" )]
			public decimal WeightedAveragePrice { get; set; }

			[JsonProperty ( "x" )]
			public decimal YesterdaysClosePrice { get; set; }

			[JsonProperty ( "c" )]
			public decimal Close { get; set; }

			[JsonProperty ( "Q" )]
			public decimal CloseTradeQuantity { get; set; }

			[JsonProperty ( "b" )]
			public decimal BestBidPrice { get; set; }

			[JsonProperty ( "B" )]
			public decimal BestBidQuantity { get; set; }

			[JsonProperty ( "a" )]
			public decimal BestAskPrice { get; set; }

			[JsonProperty ( "A" )]
			public decimal BestAskQuantity { get; set; }

			[JsonProperty ( "o" )]
			public decimal Open { get; set; }

			[JsonProperty ( "h" )]
			public decimal High { get; set; }

			[JsonProperty ( "l" )]
			public decimal Low { get; set; }

			[JsonProperty ( "v" )]
			public decimal TotalTradedBaseAssetVolume { get; set; }

			[JsonProperty ( "q" )]
			public decimal TotalTradedQuoteAssetVolume { get; set; }

			[JsonProperty ( "O" )]
			public long StatisticsOpenTime { get; set; }

			[JsonProperty ( "C" )]
			public long StatisticsCloseTime { get; set; }

			[JsonProperty ( "F" )]
			public long FirstTradeId { get; set; }

			[JsonProperty ( "L" )]
			public long LastTradeId { get; set; }

			[JsonProperty ( "n" )]
			public long NumberOfTrades { get; set; }
		}
	}
}