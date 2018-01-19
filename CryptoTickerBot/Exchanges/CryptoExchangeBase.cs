using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
		Poloniex,
	}

	public abstract class CryptoExchangeBase
	{
		public string Name { get; protected set; }
		public string Url { get; protected set; }
		public string TickerUrl { get; protected set; }
		public CryptoExchange Id { get; protected set; }
		public Dictionary<string, CryptoCoin> ExchangeData { get; protected set; }
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
					Console.WriteLine ( e );
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
	}
}