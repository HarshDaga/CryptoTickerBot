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
		private static readonly List<string> AllowedSymbols = new List<string> {"BTC", "ETH", "BCH", "LTC", "BNB"};

		public BinanceExchange ( ) : base ( CryptoExchangeId.Binance )
		{
		}

		private class BinanceTickerDatum
		{
			[JsonProperty ( "e" )]
			public string EventType { get; set; }

			[JsonProperty ( "E" )]
			public long EventTime { get; set; }

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
			public decimal TodaysClosePrice { get; set; }

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
			public decimal OpenPrice { get; set; }

			[JsonProperty ( "h" )]
			public decimal HighPrice { get; set; }

			[JsonProperty ( "l" )]
			public decimal LowPrice { get; set; }

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

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<CryptoCoinId, CryptoCoin> ( );

			using ( var ws = new WebSocket ( TickerUrl ) )
			{
				await Task.Run ( ( ) => ws.Connect ( ), ct ).ConfigureAwait ( false );

				ws.OnMessage += WsOnMessage;

				while ( ws.Ping ( ) )
					await Task.Delay ( 60000, ct ).ConfigureAwait ( false );
			}
		}

		private void WsOnMessage ( object sender, MessageEventArgs args )
		{
			try
			{
				var json = args.Data;
				var data = JsonConvert.DeserializeObject<List<BinanceTickerDatum>> ( json );

				foreach ( var datum in data )
				{
					var s = datum.Symbol;
					if ( !s.EndsWith ( "USDT" ) ) continue;
					var symbol = s.Replace ( "USDT", "" );
					if ( symbol == "BCC" ) symbol = "BCH";
					if ( !AllowedSymbols.Contains ( symbol ) ) continue;
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
			var d = (BinanceTickerDatum) datum;
			ExchangeData[id].LowestAsk  = d.BestAskPrice;
			ExchangeData[id].HighestBid = d.BestBidPrice;
			ExchangeData[id].Rate       = d.WeightedAveragePrice;
		}
	}
}