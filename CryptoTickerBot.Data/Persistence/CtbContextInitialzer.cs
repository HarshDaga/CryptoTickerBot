using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;
using NLog;

namespace CryptoTickerBot.Data.Persistence
{
	public class CtbContextInitialzer : CreateDatabaseIfNotExists<CtbContext>
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		protected override void Seed ( CtbContext context )
		{
			Logger.Info ( "Seeding database" );
			StaticSeed ( context );
			Logger.Info ( "Database ready" );
		}

		public static void StaticSeed ( CtbContext context )
		{
			context.Coins.AddOrUpdate (
				new CryptoCoin ( CryptoCoinId.BTC, "Bitcoin" ),
				new CryptoCoin ( CryptoCoinId.ETH, "Ethereum" ),
				new CryptoCoin ( CryptoCoinId.BCH, "Bitcoin Cash" ),
				new CryptoCoin ( CryptoCoinId.LTC, "Litecoin" ),
				new CryptoCoin ( CryptoCoinId.XRP, "Ripple" ),
				new CryptoCoin ( CryptoCoinId.NEO, "NEO" ),
				new CryptoCoin ( CryptoCoinId.DASH, "Dash" ),
				new CryptoCoin ( CryptoCoinId.XMR, "Monero" ),
				new CryptoCoin ( CryptoCoinId.TRX, "Tron" ),
				new CryptoCoin ( CryptoCoinId.ETC, "Ethereum Classic" ),
				new CryptoCoin ( CryptoCoinId.OMG, "OmiseGo" ),
				new CryptoCoin ( CryptoCoinId.ZEC, "Zcash" ),
				new CryptoCoin ( CryptoCoinId.XLM, "Stellar" ),
				new CryptoCoin ( CryptoCoinId.BNB, "Binance Coin" ),
				new CryptoCoin ( CryptoCoinId.BTG, "Bitcoin Gold" ),
				new CryptoCoin ( CryptoCoinId.BCD, "Bitcoin Diamond" ),
				new CryptoCoin ( CryptoCoinId.IOT, "IOTA" ),
				new CryptoCoin ( CryptoCoinId.DOGE, "Dogecoin" ),
				new CryptoCoin ( CryptoCoinId.STEEM, "Steem" )
			);

			context.Exchanges.AddOrUpdate (
				new CryptoExchange
				(
					CryptoExchangeId.Binance,
					"Binance",
					"https://www.binance.com/",
					"wss://stream2.binance.com:9443/ws/!ticker@arr@3000ms",
					0.1m,
					0.1m,
					withdrawalFees: new Dictionary<CryptoCoinId, decimal>
					{
						[CryptoCoinId.BTC] = 0.001m,
						[CryptoCoinId.ETH] = 0.01m,
						[CryptoCoinId.BCH] = 0.001m,
						[CryptoCoinId.LTC] = 0.01m
					},
					depositFees: new Dictionary<CryptoCoinId, decimal>
					{
						[CryptoCoinId.BTC] = 0,
						[CryptoCoinId.ETH] = 0,
						[CryptoCoinId.BCH] = 0,
						[CryptoCoinId.LTC] = 0
					}
				)
			);

			context.Exchanges.AddOrUpdate (
				new CryptoExchange
				(
					CryptoExchangeId.BitBay,
					"BitBay",
					"https://bitbay.net/en",
					"https://api.bitbay.net/rest/trading/ticker",
					0.3m,
					0.3m,
					withdrawalFees: new Dictionary<CryptoCoinId, decimal>
					{
						[CryptoCoinId.BTC] = 0.0009m,
						[CryptoCoinId.ETH] = 0.00126m,
						[CryptoCoinId.BCH] = 0.0006m,
						[CryptoCoinId.LTC] = 0.005m
					},
					depositFees: new Dictionary<CryptoCoinId, decimal>
					{
						[CryptoCoinId.BTC] = 0,
						[CryptoCoinId.ETH] = 0,
						[CryptoCoinId.BCH] = 0,
						[CryptoCoinId.LTC] = 0
					}
				)
			);

			context.Exchanges.AddOrUpdate (
				new CryptoExchange
				(
					CryptoExchangeId.Coinbase,
					"Coinbase",
					"https://www.coinbase.com/",
					"wss://ws-feed.gdax.com/",
					0.3m,
					0.3m,
					withdrawalFees: new Dictionary<CryptoCoinId, decimal>
					{
						[CryptoCoinId.BTC] = 0.001m,
						[CryptoCoinId.ETH] = 0.003m,
						[CryptoCoinId.BCH] = 0.001m,
						[CryptoCoinId.LTC] = 0.01m
					},
					depositFees: new Dictionary<CryptoCoinId, decimal>
					{
						[CryptoCoinId.BTC] = 0,
						[CryptoCoinId.ETH] = 0,
						[CryptoCoinId.BCH] = 0,
						[CryptoCoinId.LTC] = 0
					}
				)
			);

			context.Exchanges.AddOrUpdate (
				new CryptoExchange
				(
					CryptoExchangeId.CoinDelta,
					"CoinDelta",
					"https://coindelta.com/",
					"https://coindelta.com/api/v1/public/getticker/",
					0.3m,
					0.3m,
					withdrawalFees: new Dictionary<CryptoCoinId, decimal>
					{
						[CryptoCoinId.BTC] = 0.001m,
						[CryptoCoinId.ETH] = 0.001m,
						[CryptoCoinId.BCH] = 0.001m,
						[CryptoCoinId.LTC] = 0.002m
					},
					depositFees: new Dictionary<CryptoCoinId, decimal>
					{
						[CryptoCoinId.BTC] = 0,
						[CryptoCoinId.ETH] = 0,
						[CryptoCoinId.BCH] = 0,
						[CryptoCoinId.LTC] = 0
					}
				)
			);

			context.Exchanges.AddOrUpdate (
				new CryptoExchange
				(
					CryptoExchangeId.Koinex,
					"Koinex",
					"https://koinex.in/",
					"wss://ws-ap2.pusher.com/app/9197b0bfdf3f71a4064e?protocol=7&client=js&version=4.1.0&flash=false",
					0.25m,
					withdrawalFees: new Dictionary<CryptoCoinId, decimal>
					{
						[CryptoCoinId.BTC] = 0.001m,
						[CryptoCoinId.ETH] = 0.003m,
						[CryptoCoinId.BCH] = 0.001m,
						[CryptoCoinId.LTC] = 0.01m
					},
					depositFees: new Dictionary<CryptoCoinId, decimal>
					{
						[CryptoCoinId.BTC] = 0,
						[CryptoCoinId.ETH] = 0,
						[CryptoCoinId.BCH] = 0,
						[CryptoCoinId.LTC] = 0
					}
				)
			);

			context.Exchanges.AddOrUpdate (
				new CryptoExchange
				(
					CryptoExchangeId.Kraken,
					"Kraken",
					"https://www.kraken.com/",
					"https://api.kraken.com/0/public/Ticker?pair=XBTUSD,BCHUSD,ETHUSD,LTCUSD",
					0.26m,
					0.26m,
					withdrawalFees: new Dictionary<CryptoCoinId, decimal>
					{
						[CryptoCoinId.BTC] = 0.0025m,
						[CryptoCoinId.ETH] = 0.005m,
						[CryptoCoinId.BCH] = 0.001m,
						[CryptoCoinId.LTC] = 0.02m
					},
					depositFees: new Dictionary<CryptoCoinId, decimal>
					{
						[CryptoCoinId.BTC] = 0,
						[CryptoCoinId.ETH] = 0,
						[CryptoCoinId.BCH] = 0,
						[CryptoCoinId.LTC] = 0
					}
				)
			);

			context.TeleBotUsers.AddOrUpdate ( new TeleBotUser ( "DevilDaga", UserRole.Owner ) );
		}
	}
}