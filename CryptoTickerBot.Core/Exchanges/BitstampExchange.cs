using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Core.Converters;
using CryptoTickerBot.Enums;
using Flurl.Http;
using Newtonsoft.Json;
using PurePusher;

namespace CryptoTickerBot.Core.Exchanges
{
	public class BitstampExchange : CryptoExchangeBase<dynamic>
	{
		private const string TradingPairsEndpoint = "https://www.bitstamp.net/api/v2/trading-pairs-info/";
		private const string TickerEndpoint = "https://www.bitstamp.net/api/v2/ticker/";

		public PurePusherClient Client { get; private set; }
		public List<BitstampAsset> Assets { get; private set; }

		public BitstampExchange ( ) : base ( CryptoExchangeId.Bitstamp )
		{
		}

		private async Task FetchInitialData ( CancellationToken ct )
		{
			foreach ( var asset in Assets )
			{
				var datum = await $"{TickerEndpoint}{asset.UrlSymbol}/"
					.GetJsonAsync<TickerDatum> ( ct );

				var symbol = asset.Name;
				ExchangeData[symbol] =
					new CryptoCoin ( symbol, datum.Bid, datum.Ask, datum.Last, datum.Timestamp );

				LastUpdate = DateTime.UtcNow;
				OnNext ( ExchangeData[symbol] );
				OnChanged ( ExchangeData[symbol] );

				await Task.Delay ( PollingRate, ct );
			}
		}

		protected override async Task GetExchangeData ( CancellationToken ct )
		{
			Assets = await TradingPairsEndpoint.GetJsonAsync<List<BitstampAsset>> ( ct );

			await FetchInitialData ( ct );

			Client = new PurePusherClient ( TickerUrl, new PurePusherClientOptions
			{
				DebugMode = false
			} );

			if ( !Client.Connect ( ) )
				return;

			Client.Connected += sender =>
			{
				foreach ( var asset in Assets )
					Client
						.Subscribe ( $"live_trades_{asset.UrlSymbol}" )
						.Bind ( "trade", o => Update ( o, asset.Name ) );
			};

			while ( Client.Connection.State != WebSocketState.Closed )
				await Task.Delay ( PollingRate, ct );
		}

		protected override void Update ( dynamic data,
		                                 string symbol )
		{
			if ( ExchangeData.TryGetValue ( symbol, out var old ) )
				old = old.Clone ( );
			else
				ExchangeData[symbol] = new CryptoCoin ( symbol );

			DeserializeData ( data, symbol );

			LastUpdate = DateTime.UtcNow;
			OnNext ( ExchangeData[symbol] );

			if ( ExchangeData[symbol] != old )
				OnChanged ( ExchangeData[symbol] );
		}

		protected override void DeserializeData ( dynamic data,
		                                          string id )
		{
			var price = (decimal) data["price"];

			if ( data["type"] == 1 )
				ExchangeData[id].HighestBid = price;
			else
				ExchangeData[id].LowestAsk = price;

			ExchangeData[id].Rate = price;
			ExchangeData[id].Time = DateTimeOffset
				.FromUnixTimeSeconds ( long.Parse ( data["timestamp"] ) )
				.UtcDateTime;
		}

		public override Task StopReceivingAsync ( )
		{
			Client?.Disconnect ( );
			return base.StopReceivingAsync ( );
		}

		#region JSON Classes

		public class TickerDatum
		{
			[JsonProperty ( "high" )]
			public decimal High { get; set; }

			[JsonProperty ( "last" )]
			public decimal Last { get; set; }

			[JsonProperty ( "timestamp" )]
			[JsonConverter ( typeof ( StringDateTimeConverter ) )]
			public DateTime Timestamp { get; set; }

			[JsonProperty ( "bid" )]
			public decimal Bid { get; set; }

			[JsonProperty ( "vwap" )]
			public decimal VolumeWeightedAvgPrice { get; set; }

			[JsonProperty ( "volume" )]
			public decimal Volume { get; set; }

			[JsonProperty ( "low" )]
			public decimal Low { get; set; }

			[JsonProperty ( "ask" )]
			public decimal Ask { get; set; }

			[JsonProperty ( "open" )]
			public decimal Open { get; set; }
		}

		public class BitstampAsset
		{
			[JsonProperty ( "base_decimals" )]
			public long BaseDecimals { get; set; }

			[JsonProperty ( "minimum_order" )]
			public string MinimumOrder { get; set; }

			[JsonProperty ( "name" )]
			public string Name { get; set; }

			[JsonProperty ( "counter_decimals" )]
			public long CounterDecimals { get; set; }

			[JsonProperty ( "trading" )]
			public string Trading { get; set; }

			[JsonProperty ( "url_symbol" )]
			public string UrlSymbol { get; set; }

			[JsonProperty ( "description" )]
			public string Description { get; set; }

			public override string ToString ( ) => Name;
		}

		#endregion
	}
}