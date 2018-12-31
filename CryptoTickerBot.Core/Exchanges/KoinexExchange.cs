using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Core.Helpers;
using CryptoTickerBot.Data.Converters;
using CryptoTickerBot.Data.Domain;
using Newtonsoft.Json;
using NLog;

namespace CryptoTickerBot.Core.Exchanges
{
	public class KoinexExchange : CryptoExchangeBase<KoinexExchange.KoinexCoin>
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public KoinexExchange ( ) : base ( CryptoExchangeId.Koinex )
		{
		}

		protected override async Task GetExchangeDataAsync ( CancellationToken ct )
		{
			while ( !ct.IsCancellationRequested )
			{
				try
				{
					var json = await Utility.DownloadWebPageAsync ( TickerUrl ).ConfigureAwait ( false );
					var data = JsonConvert.DeserializeObject<KoinexTicker> ( json );

					foreach ( var kp in data.Stats.Inr )
						Update ( kp.Value, $"{kp.Key}INR" );
				}
				catch ( Exception e )
				{
					Logger.Error ( e );
				}

				await Task.Delay ( PollingRate, ct ).ConfigureAwait ( false );
			}
		}

		protected override void DeserializeData ( KoinexCoin data,
		                                          string id )
		{
			ExchangeData[id].LowestAsk  = data.LowestAsk;
			ExchangeData[id].HighestBid = data.HighestBid;
			ExchangeData[id].Rate       = data.LastTradedPrice;
		}

		#region JSON Classes

		public class KoinexTicker
		{
			[JsonProperty ( "prices" )]
			public Prices Prices { get; set; }

			[JsonProperty ( "stats" )]
			public Stats Stats { get; set; }
		}

		public class Prices
		{
			[JsonProperty ( "inr" )]
			public Dictionary<string, string> Inr { get; set; }

			[JsonProperty ( "bitcoin" )]
			public Dictionary<string, string> Bitcoin { get; set; }

			[JsonProperty ( "ether" )]
			public Dictionary<string, string> Ether { get; set; }

			[JsonProperty ( "ripple" )]
			public Dictionary<string, string> Ripple { get; set; }
		}

		public class Stats
		{
			[JsonProperty ( "inr" )]
			public Dictionary<string, KoinexCoin> Inr { get; set; }

			[JsonProperty ( "bitcoin" )]
			public Dictionary<string, KoinexCoin> Bitcoin { get; set; }

			[JsonProperty ( "ether" )]
			public Dictionary<string, KoinexCoin> Ether { get; set; }

			[JsonProperty ( "ripple" )]
			public Dictionary<string, KoinexCoin> Ripple { get; set; }
		}

		public class KoinexCoin
		{
			[JsonProperty ( "highest_bid" )]
			[JsonConverter ( typeof ( DecimalConverter ) )]
			public decimal HighestBid { get; set; }

			[JsonProperty ( "lowest_ask" )]
			[JsonConverter ( typeof ( DecimalConverter ) )]
			public decimal LowestAsk { get; set; }

			[JsonProperty ( "last_traded_price" )]
			[JsonConverter ( typeof ( DecimalConverter ) )]
			public decimal LastTradedPrice { get; set; }

			[JsonProperty ( "min_24hrs" )]
			[JsonConverter ( typeof ( DecimalConverter ) )]
			public decimal Min24Hrs { get; set; }

			[JsonProperty ( "max_24hrs" )]
			[JsonConverter ( typeof ( DecimalConverter ) )]
			public decimal Max24Hrs { get; set; }

			[JsonProperty ( "vol_24hrs" )]
			[JsonConverter ( typeof ( DecimalConverter ) )]
			public decimal Vol24Hrs { get; set; }

			[JsonProperty ( "currency_full_form" )]
			public string CurrencyFullForm { get; set; }

			[JsonProperty ( "currency_short_form" )]
			public string CurrencyShortForm { get; set; }

			[JsonProperty ( "per_change" )]
			[JsonConverter ( typeof ( DecimalConverter ) )]
			public decimal PerChange { get; set; }

			[JsonProperty ( "trade_volume" )]
			[JsonConverter ( typeof ( DecimalConverter ) )]
			public decimal TradeVolume { get; set; }
		}

		#endregion
	}
}