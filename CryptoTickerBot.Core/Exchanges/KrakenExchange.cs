using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Domain;
using Flurl.Http;
using Newtonsoft.Json;
using NLog;
using Polly;

namespace CryptoTickerBot.Core.Exchanges
{
	public class KrakenExchange : CryptoExchangeBase<KrakenExchange.KrakenCoinInfo>
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public KrakenAssetPairs Assets { get; private set; }
		public readonly string TradableAssetPairsEndpoint;
		public readonly string TickerEndpoint;

		protected Policy RetryPolicy;

		public KrakenExchange ( ) : base ( CryptoExchangeId.Kraken )
		{
			TradableAssetPairsEndpoint = $"{TickerUrl}0/public/AssetPairs";
			TickerEndpoint             = $"{TickerUrl}0/public/Ticker";

			RetryPolicy = Policy
				.Handle<FlurlHttpException> ( )
				.WaitAndRetryAsync ( 5, i => CooldownPeriod );
		}

		protected override async Task GetExchangeData ( CancellationToken ct )
		{
			Assets = await TradableAssetPairsEndpoint
				.GetJsonAsync<KrakenAssetPairs> ( ct )
				.ConfigureAwait ( false );
			var tickerUrlWithPairs = $"{TickerEndpoint}?pair={string.Join ( ",", Assets.Result.Keys )}";

			while ( !ct.IsCancellationRequested )
			{
				try
				{
					var data = await RetryPolicy
						.ExecuteAsync ( async ( ) =>
							                await tickerUrlWithPairs
								                .GetJsonAsync<Root> ( ct )
								                .ConfigureAwait ( false ) );

					foreach ( var kp in data.Results )
						Update ( kp.Value, kp.Key );
				}
				catch ( Exception e )
				{
					Logger.Error ( e );
				}

				await Task.Delay ( PollingRate, ct ).ConfigureAwait ( false );
			}
		}

		protected override void DeserializeData ( KrakenCoinInfo data,
		                                          string id )
		{
			ExchangeData[id].LowestAsk  = data.Ask[0];
			ExchangeData[id].HighestBid = data.Bid[0];
			ExchangeData[id].Rate       = data.LastTrade[0];
		}

		#region JSON Structure

		public class KrakenAssetPairs
		{
			[JsonProperty ( "error" )]
			public object[] Error { get; set; }

			[JsonProperty ( "result" )]
			public Dictionary<string, KrakenAssetPair> Result { get; set; }
		}

		public class KrakenAssetPair
		{
			[JsonProperty ( "altname" )]
			public string AltName { get; set; }

			[JsonProperty ( "aclass_base" )]
			public string BaseAssetClass { get; set; }

			[JsonProperty ( "base" )]
			public string Base { get; set; }

			[JsonProperty ( "aclass_quote" )]
			public string QuoteAssetClass { get; set; }

			[JsonProperty ( "quote" )]
			public string Quote { get; set; }

			[JsonProperty ( "lot" )]
			public string Lot { get; set; }

			[JsonProperty ( "pair_decimals" )]
			public long PairDecimals { get; set; }

			[JsonProperty ( "lot_decimals" )]
			public long LotDecimals { get; set; }

			[JsonProperty ( "lot_multiplier" )]
			public long LotMultiplier { get; set; }

			[JsonProperty ( "leverage_buy" )]
			public long[] LeverageBuy { get; set; }

			[JsonProperty ( "leverage_sell" )]
			public long[] LeverageSell { get; set; }

			[JsonProperty ( "fees" )]
			public decimal[][] Fees { get; set; }

			[JsonProperty ( "fees_maker", NullValueHandling = NullValueHandling.Ignore )]
			public decimal[][] FeesMaker { get; set; }

			[JsonProperty ( "fee_volume_currency" )]
			public string FeeVolumeCurrency { get; set; }

			[JsonProperty ( "margin_call" )]
			public long MarginCall { get; set; }

			[JsonProperty ( "margin_stop" )]
			public long MarginStop { get; set; }
		}

		private class Root
		{
			[JsonProperty ( "error" )]
			public List<object> Error { get; set; }

			[JsonProperty ( "result" )]
			public Dictionary<string, KrakenCoinInfo> Results { get; set; }
		}

		public class KrakenCoinInfo
		{
			[JsonProperty ( "a" )]
			public List<decimal> Ask { get; set; }

			[JsonProperty ( "b" )]
			public List<decimal> Bid { get; set; }

			[JsonProperty ( "c" )]
			public List<decimal> LastTrade { get; set; }

			[JsonProperty ( "v" )]
			public List<decimal> Volume { get; set; }

			[JsonProperty ( "p" )]
			public List<decimal> Price { get; set; }

			[JsonProperty ( "t" )]
			public List<decimal> Trades { get; set; }

			[JsonProperty ( "l" )]
			public List<decimal> Low { get; set; }

			[JsonProperty ( "h" )]
			public List<decimal> High { get; set; }

			[JsonProperty ( "o" )]
			public decimal Open { get; set; }
		}

		#endregion
	}
}