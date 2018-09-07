using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CryptoTickerBot.Collections;
using CryptoTickerBot.Domain;
using CryptoTickerBot.Domain.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CryptoTickerBot.Core
{
	public class CoreConfig : IConfig
	{
		public string ConfigFileName { get; } = "CoreConfig";

		public string FixerApiKey { get; set; }

		[SuppressMessage ( "ReSharper", "StringLiteralTypo" )]
		public List<CryptoExchangeApiInfo> ExchangeApiInfo { get; set; } = new List<CryptoExchangeApiInfo>
		{
			new CryptoExchangeApiInfo
			{
				Id             = CryptoExchangeId.Binance,
				Name           = "Binance",
				Url            = "https://www.binance.com/",
				TickerUrl      = "wss://stream2.binance.com:9443/ws/!ticker@arr",
				BuyFees        = 0.1m,
				SellFees       = 0.1m,
				PollingRate    = TimeSpan.FromMilliseconds ( 1000 ),
				CooldownPeriod = TimeSpan.FromSeconds ( 5 ),
				SymbolMappings = new OrderedDictionary<string, string>
				{
					["BCC"] = "BCH"
				},
				BaseSymbols = new List<string> {"USDT", "BTC", "ETH", "BNB"}
			},
			new CryptoExchangeApiInfo
			{
				Id          = CryptoExchangeId.Coinbase,
				Name        = "Coinbase",
				Url         = "https://www.coinbase.com/",
				TickerUrl   = "wss://ws-feed.pro.coinbase.com",
				BuyFees     = 0.3m,
				SellFees    = 0.3m,
				BaseSymbols = new List<string> {"BTC", "USD", "EUR", "GBP"}
			},
			new CryptoExchangeApiInfo
			{
				Id          = CryptoExchangeId.CoinDelta,
				Name        = "CoinDelta",
				Url         = "https://coindelta.com/",
				TickerUrl   = "https://api.coindelta.com/api/v1/public/getticker/",
				BuyFees     = 0.3m,
				SellFees    = 0.3m,
				PollingRate = TimeSpan.FromSeconds ( 60 ),
				BaseSymbols = new List<string> {"USDT", "INR"}
			},
			new CryptoExchangeApiInfo
			{
				Id          = CryptoExchangeId.Koinex,
				Name        = "Koinex",
				Url         = "https://koinex.in/",
				TickerUrl   = "https://koinex.in/api/ticker",
				BuyFees     = 0.25m,
				SellFees    = 0m,
				PollingRate = TimeSpan.FromSeconds ( 2 ),
				BaseSymbols = new List<string> {"INR"}
			},
			new CryptoExchangeApiInfo
			{
				Id             = CryptoExchangeId.Kraken,
				Name           = "Kraken",
				Url            = "https://www.kraken.com/",
				TickerUrl      = "https://api.kraken.com/",
				BuyFees        = 0.26m,
				SellFees       = 0.26m,
				CooldownPeriod = TimeSpan.FromSeconds ( 10 ),
				SymbolMappings = new OrderedDictionary<string, string>
				{
					["ZUSD"] = "USD",
					["ZEUR"] = "EUR",
					["ZCAD"] = "CAD",
					["ZGBP"] = "GBP",
					["ZJPY"] = "JPY",
					["XXBT"] = "BTC",
					["XBT"]  = "BTC",
					["XETH"] = "ETH",
					["XLTC"] = "LTC",
					["XETC"] = "ETC",
					["XICN"] = "ICN",
					["XMLN"] = "MLN",
					["XREP"] = "REP",
					["XXDG"] = "XDG",
					["XXLM"] = "XLM",
					["XXMR"] = "XMR",
					["XXRP"] = "XRP",
					["XZEC"] = "ZEC"
				},
				BaseSymbols = new List<string> {"BTC", "ETH", "USD", "EUR", "GBP", "CAD", "JPY"}
			},

			new CryptoExchangeApiInfo
			{
				Id          = CryptoExchangeId.Bitstamp,
				Name        = "Bitstamp",
				Url         = "https://www.bitstamp.net/",
				TickerUrl   = "de504dc5763aeef9ff52",
				PollingRate = TimeSpan.FromSeconds ( 1.5 ),
				BaseSymbols = new List<string> {"BTC", "USD", "EUR"}
			},

			new CryptoExchangeApiInfo
			{
				Id             = CryptoExchangeId.Zebpay,
				Name           = "Zebpay",
				Url            = "https://www.zebpay.com/",
				TickerUrl      = "https://www.zebapi.com/api/v1/market/ticker-new/",
				PollingRate    = TimeSpan.FromSeconds ( 5 ),
				CooldownPeriod = TimeSpan.FromMinutes ( 5 ),
				BaseSymbols    = new List<string> {"INR"}
			}
		};

		public class CryptoExchangeApiInfo
		{
			[JsonConverter ( typeof ( StringEnumConverter ) )]
			public CryptoExchangeId Id { get; set; }

			public string Name { get; set; }

			public string Url { get; set; }

			public string TickerUrl { get; set; }

			public OrderedDictionary<string, string> SymbolMappings { get; set; } =
				new OrderedDictionary<string, string> ( );

			public List<string> BaseSymbols { get; set; } = new List<string> ( );

			public TimeSpan PollingRate { get; set; } = TimeSpan.FromSeconds ( 5 );
			public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromSeconds ( 60 );

			public decimal BuyFees { get; set; }
			public decimal SellFees { get; set; }
			public Dictionary<string, decimal> DepositFees { get; set; } = new Dictionary<string, decimal> ( );
			public Dictionary<string, decimal> WithdrawalFees { get; set; } = new Dictionary<string, decimal> ( );
		}
	}
}