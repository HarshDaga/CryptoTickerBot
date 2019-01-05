using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Data.Domain;
using Flurl.Http;
using Newtonsoft.Json;
using NLog;
using PureWebSockets;

namespace CryptoTickerBot.Core.Exchanges
{
	public class BinanceExchange : CryptoExchangeBase<BinanceExchange.ITickerDatum>
	{
		public const string RestBaseEndpoint = "https://api.binance.com";
		public const string RestTickerEndpoint = "/api/v1/ticker/24hr";

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public BinanceExchange ( ) : base ( CryptoExchangeId.Binance )
		{
			TickerUrl = $"{TickerUrl}@{PollingRate.TotalMilliseconds}ms";
		}

		protected override async Task FetchInitialDataAsync ( CancellationToken ct )
		{
			try
			{
				var data = await $"{RestBaseEndpoint}{RestTickerEndpoint}"
					.GetJsonAsync<List<RestTickerDatum>> ( ct )
					.ConfigureAwait ( false );

				foreach ( var datum in data )
					Update ( datum, datum.Symbol );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		protected override async Task GetExchangeDataAsync ( CancellationToken ct )
		{
			var options = new PureWebSocketOptions
			{
				DebugMode = false
			};

			using ( var ws = new PureWebSocket ( TickerUrl, options ) )
			{
				var closed = false;

				ws.OnMessage += WsOnMessage;
				ws.OnClosed += reason =>
				{
					Logger.Info ( $"Binance closed: {reason}" );
					closed = true;
				};
				ws.OnError += exception =>
				{
					Logger.Error ( exception );
					closed = true;
				};

				if ( !ws.Connect ( ) )
					Logger.Error ( "Couldn't connect to Binance" );

				while ( ws.State != WebSocketState.Closed )
				{
					if ( UpTime > LastUpdateDuration &&
					     LastUpdateDuration > TimeSpan.FromHours ( 1 ) ||
					     closed )
					{
						ws.Disconnect ( );
						break;
					}

					await Task.Delay ( PollingRate, ct ).ConfigureAwait ( false );
				}
			}
		}

		protected override void DeserializeData ( ITickerDatum datum,
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
				var data = JsonConvert.DeserializeObject<List<WebsocketTickerDatum>> ( json );

				foreach ( var datum in data )
					Update ( datum, datum.Symbol );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		public interface ITickerDatum
		{
			string Symbol { get; set; }
			decimal Close { get; set; }
			decimal BestBidPrice { get; set; }
			decimal BestAskPrice { get; set; }
		}

		public class RestTickerDatum : ITickerDatum
		{
			[JsonProperty ( "symbol" )]
			public string Symbol { get; set; }

			[JsonProperty ( "priceChange" )]
			public decimal PriceChange { get; set; }

			[JsonProperty ( "priceChangePercent" )]
			public decimal PriceChangePercent { get; set; }

			[JsonProperty ( "weightedAvgPrice" )]
			public decimal WeightedAvgPrice { get; set; }

			[JsonProperty ( "prevClosePrice" )]
			public decimal PrevClosePrice { get; set; }

			[JsonProperty ( "lastPrice" )]
			public decimal Close { get; set; }

			[JsonProperty ( "lastQty" )]
			public decimal LastQty { get; set; }

			[JsonProperty ( "bidPrice" )]
			public decimal BestBidPrice { get; set; }

			[JsonProperty ( "askPrice" )]
			public decimal BestAskPrice { get; set; }

			[JsonProperty ( "openPrice" )]
			public decimal OpenPrice { get; set; }

			[JsonProperty ( "highPrice" )]
			public decimal HighPrice { get; set; }

			[JsonProperty ( "lowPrice" )]
			public decimal LowPrice { get; set; }

			[JsonProperty ( "volume" )]
			public decimal Volume { get; set; }

			[JsonProperty ( "quoteVolume" )]
			public decimal QuoteVolume { get; set; }

			[JsonProperty ( "openTime" )]
			public long OpenTime { get; set; }

			[JsonProperty ( "closeTime" )]
			public long CloseTime { get; set; }

			[JsonProperty ( "firstId" )]
			public long FirstId { get; set; }

			[JsonProperty ( "lastId" )]
			public long LastId { get; set; }

			[JsonProperty ( "count" )]
			public long Count { get; set; }
		}

		public class WebsocketTickerDatum : ITickerDatum
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