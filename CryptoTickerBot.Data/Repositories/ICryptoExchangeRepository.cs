using System;
using System.Collections.Generic;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;

namespace CryptoTickerBot.Data.Repositories
{
	public interface ICryptoExchangeRepository : IRepository<CryptoExchange>
	{
		CryptoExchange Get ( CryptoExchangeId id );

		void AddExchange (
			CryptoExchangeId id,
			string name,
			string url,
			string tickerUrl,
			decimal buyFees = 0,
			decimal sellFees = 0,
			DateTime? lastUpdate = null,
			DateTime? lastChange = null,
			IDictionary<CryptoCoinId, decimal> withdrawalFees = null,
			IDictionary<CryptoCoinId, decimal> depositFees = null
		);

		bool UpdateExchange (
			CryptoExchangeId id,
			string name = null,
			string url = null,
			string tickerUrl = null,
			decimal buyFees = -1,
			decimal sellFees = -1,
			DateTime? lastUpdate = null,
			DateTime? lastChange = null,
			IDictionary<CryptoCoinId, decimal> withdrawalFees = null,
			IDictionary<CryptoCoinId, decimal> depositFees = null
		);
	}
}