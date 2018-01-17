﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoTickerBot.Exchanges
{
	public abstract class CryptoExchangeBase
	{
		public string Name { get; protected set; }
		public Uri Url { get; protected set; }
		public Uri TickerUrl { get; protected set; }
		public Dictionary<string, CryptoCoin> ExchangeData { get; protected set; }
		public abstract Task GetExchangeData ( CancellationToken ct );

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