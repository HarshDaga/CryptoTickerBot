using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Helpers;
using NLog;
using Tababular;

namespace CryptoTickerBot.Exchanges
{
	public enum CryptoExchange
	{
		Koinex,
		BitBay,
		Binance,
		CoinDelta,
		Coinbase,
		Kraken,
		Bitstamp,
		Bitfinex,
		Poloniex
	}

	public abstract class CryptoExchangeBase
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public string Name { get; protected set; }
		public string Url { get; protected set; }
		public string TickerUrl { get; protected set; }
		public CryptoExchange Id { get; protected set; }
		public Dictionary<string, CryptoCoin> ExchangeData { get; protected set; }
		public Dictionary<string, decimal> DepositFees { get; protected set; }
		public Dictionary<string, decimal> WithdrawalFees { get; protected set; }
		public decimal BuyFees { get; protected set; }
		public decimal SellFees { get; protected set; }
		public bool IsComplete => ExchangeData.Count == KnownSymbols.Count;
		public DateTime LastUpdate { get; protected set; }
		public abstract Task GetExchangeData ( CancellationToken ct );

		public async Task StartMonitor ( CancellationToken ct = default )
		{
			while ( !ct.IsCancellationRequested )
			{
				try
				{
					await GetExchangeData ( ct );
				}
				catch ( Exception e )
				{
					Logger.Error ( e );
					await Task.Delay ( 2000, ct );
				}
			}
		}

		protected static readonly List<string> KnownSymbols = new List<string>
		{
			"BTC",
			"LTC",
			"ETH",
			"BCH"
		};

		protected CryptoExchangeBase ( )
		{
			ExchangeData = new Dictionary<string, CryptoCoin> ( );
		}

		protected abstract void Update ( dynamic data, string symbol );

		protected void ApplyFees ( string symbol )
		{
			var coin = ExchangeData[symbol];
			coin.LowestAsk += coin.LowestAsk * BuyFees / 100m;
			coin.HighestBid += coin.HighestBid * SellFees / 100m;
			ExchangeData[symbol] = coin;
		}

		public CryptoCoin this [ string symbol ]
		{
			get => ExchangeData[symbol];
			set => ExchangeData[symbol] = value;
		}

		public List<IList<object>> ToSheetRows ( ) =>
			ExchangeData.Values.OrderBy ( coin => coin.Symbol ).Select ( coin => coin.ToSheetsRow ( ) ).ToList ( );

		public event Action<CryptoExchangeBase, CryptoCoin> Changed;

		public void OnChanged ( CryptoExchangeBase exchange, CryptoCoin coin ) =>
			Changed?.Invoke ( exchange, coin );

		public override string ToString ( )
		{
			var formatter = new TableFormatter ( );
			var objects = new List<object> ( );

			foreach ( var coin in ExchangeData.Values.OrderBy ( x => x.Symbol ) )
			{
				objects.Add ( new
				{
					coin.Symbol,
					Bid = $"{coin.HighestBid:C}",
					Ask = $"{coin.LowestAsk:C}",
					Spread = $"{coin.SpreadPercentange:P}"
				} );
			}

			return formatter.FormatObjects ( objects );
		}

		public string ToString ( FiatCurrency fiat )
		{
			var formatter = new TableFormatter ( );
			var objects = new List<object> ( );

			foreach ( var coin in ExchangeData.Values.OrderBy ( x => x.Symbol ) )
			{
				objects.Add ( new
				{
					coin.Symbol,
					Bid = $"{FiatConverter.ToString ( coin.HighestBid, FiatCurrency.USD, fiat )}",
					Ask = $"{FiatConverter.ToString ( coin.LowestAsk, FiatCurrency.USD, fiat )}",
					Spread = $"{coin.SpreadPercentange:P}"
				} );
			}

			return formatter.FormatObjects ( objects );
		}
	}
}