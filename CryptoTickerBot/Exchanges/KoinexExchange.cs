using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Exchanges.Core;
using CryptoTickerBot.Helpers;
using Newtonsoft.Json;
using NLog;
using static Newtonsoft.Json.JsonConvert;
using static CryptoTickerBot.Helpers.Utility;

namespace CryptoTickerBot.Exchanges
{
	public class KoinexExchange : CryptoExchangeBase
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public KoinexExchange ( ) : base ( CryptoExchangeId.Koinex )
		{
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new ConcurrentDictionary<CryptoCoinId, CryptoCoin> ( );

			while ( !ct.IsCancellationRequested )
			{
				try
				{
					var json = DownloadWebPage ( TickerUrl );
					var data = DeserializeObject<Root> ( json );

					Update ( data.Stats.Inr.Btc, "BTC" );
					Update ( data.Stats.Inr.Bch, "BCH" );
					Update ( data.Stats.Inr.Eth, "ETH" );
					Update ( data.Stats.Inr.Ltc, "LTC" );
				}
				catch ( Exception e )
				{
					Logger.Error ( e );
				}

				await Task.Delay ( 2000, ct ).ConfigureAwait ( false );
			}
		}

		protected override void DeserializeData ( dynamic data, CryptoCoinId id )
		{
			var stats = (CoinStats) data;
			decimal InrToUsd ( decimal amount ) => FiatConverter.Convert ( amount, FiatCurrency.INR, FiatCurrency.USD );

			ExchangeData[id].LowestAsk  = InrToUsd ( stats.LowestAsk ?? 0 );
			ExchangeData[id].HighestBid = InrToUsd ( stats.HighestBid ?? 0 );
			ExchangeData[id].Rate       = InrToUsd ( stats.LastTradedPrice ?? 0 );
		}

		#region JSON Structure

		public class Root
		{
			[JsonProperty ( "prices" )]
			public Prices Prices { get; set; }

			[JsonProperty ( "stats" )]
			public Stats Stats { get; set; }
		}

		public class Prices
		{
			[JsonProperty ( "inr" )]
			public Dictionary<string, decimal> Inr { get; set; }

			[JsonProperty ( "bitcoin" )]
			public Dictionary<string, decimal> Bitcoin { get; set; }

			[JsonProperty ( "ether" )]
			public Dictionary<string, decimal> Ether { get; set; }

			[JsonProperty ( "ripple" )]
			public Dictionary<string, decimal> Ripple { get; set; }
		}

		public class Stats
		{
			[JsonProperty ( "inr" )]
			public AllCoinStats Inr { get; set; }

			[JsonProperty ( "bitcoin" )]
			public AllCoinStats Bitcoin { get; set; }

			[JsonProperty ( "ether" )]
			public AllCoinStats Ether { get; set; }

			[JsonProperty ( "ripple" )]
			public AllCoinStats Ripple { get; set; }
		}

		public class CoinStats
		{
			[JsonProperty ( "currency_full_form" )]
			public string CurrencyFullForm { get; set; }

			[JsonProperty ( "currency_short_form" )]
			public string CurrencyShortForm { get; set; }

			[JsonProperty ( "per_change" )]
			public decimal? PerChange { get; set; }

			[JsonProperty ( "last_traded_price" )]
			public decimal? LastTradedPrice { get; set; }

			[JsonProperty ( "lowest_ask" )]
			public decimal? LowestAsk { get; set; }

			[JsonProperty ( "highest_bid" )]
			public decimal? HighestBid { get; set; }

			[JsonProperty ( "min_24hrs" )]
			public decimal? Min24Hrs { get; set; }

			[JsonProperty ( "max_24hrs" )]
			public decimal? Max24Hrs { get; set; }

			[JsonProperty ( "vol_24hrs" )]
			public decimal? Vol24Hrs { get; set; }
		}

		public class AllCoinStats
		{
			[JsonProperty ( "ETH" )]
			public CoinStats Eth { get; set; }

			[JsonProperty ( "BTC" )]
			public CoinStats Btc { get; set; }

			[JsonProperty ( "LTC" )]
			public CoinStats Ltc { get; set; }

			[JsonProperty ( "XRP" )]
			public CoinStats Xrp { get; set; }

			[JsonProperty ( "BCH" )]
			public CoinStats Bch { get; set; }

			[JsonProperty ( "OMG" )]
			public CoinStats Omg { get; set; }

			[JsonProperty ( "REQ" )]
			public CoinStats Req { get; set; }

			[JsonProperty ( "ZRX" )]
			public CoinStats Zrx { get; set; }

			[JsonProperty ( "GNT" )]
			public CoinStats Gnt { get; set; }

			[JsonProperty ( "BAT" )]
			public CoinStats Bat { get; set; }

			[JsonProperty ( "AE" )]
			public CoinStats Ae { get; set; }

			[JsonProperty ( "TRX" )]
			public CoinStats Trx { get; set; }

			[JsonProperty ( "XLM" )]
			public CoinStats Xlm { get; set; }

			[JsonProperty ( "NEO" )]
			public CoinStats Neo { get; set; }

			[JsonProperty ( "GAS" )]
			public CoinStats Gas { get; set; }

			[JsonProperty ( "XRB" )]
			public CoinStats Xrb { get; set; }

			[JsonProperty ( "NCASH" )]
			public CoinStats Ncash { get; set; }

			[JsonProperty ( "AION" )]
			public CoinStats Aion { get; set; }

			[JsonProperty ( "EOS" )]
			public CoinStats Eos { get; set; }

			[JsonProperty ( "ONT" )]
			public CoinStats Ont { get; set; }
		}

		#endregion
	}
}