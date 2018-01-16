using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoTickerBot.Exchanges
{
	public interface ICryptoExchange
	{
		string Name { get; }
		Uri Url { get; }
		Uri TickerUrl { get; }

		Dictionary<string, CryptoCoin> ExchangeData { get; }

		Task GetExchangeData ( CancellationToken ct );

		event Action<ICryptoExchange, CryptoCoin> OnChanged;
	}
}