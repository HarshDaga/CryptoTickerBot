using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;

namespace CryptoTickerBot.Data.Repositories
{
	public interface ICryptoCoinValueRepository : IRepository<CryptoCoinValue>
	{
		Task<IEnumerable<CryptoCoinValue>> GetAllAsync ( CryptoCoinId coinId,
		                                                 CancellationToken cancellationToken );

		Task<IEnumerable<CryptoCoinValue>> GetAllAsync ( CryptoExchangeId exchangeId,
		                                                 CancellationToken cancellationToken );

		Task<IEnumerable<CryptoCoinValue>> GetAllAsync ( CryptoCoinId coinId,
		                                                 CryptoExchangeId exchangeId,
		                                                 CancellationToken cancellationToken );

		Task<IEnumerable<CryptoCoinValue>> GetAllAsync ( int count,
		                                                 CancellationToken cancellationToken );

		Task<IEnumerable<CryptoCoinValue>> GetLastAsync ( CryptoCoinId coinId,
		                                                  int count,
		                                                  CancellationToken cancellationToken );

		Task<IEnumerable<CryptoCoinValue>> GetLastAsync ( CryptoExchangeId exchangeId,
		                                                  int count,
		                                                  CancellationToken cancellationToken );

		Task<IEnumerable<CryptoCoinValue>> GetLastAsync ( CryptoCoinId coinId,
		                                                  CryptoExchangeId exchangeId,
		                                                  int count,
		                                                  CancellationToken cancellationToken );

		CryptoCoinValue AddCoinValue (
			CryptoCoinId coinId,
			CryptoExchangeId exchangeId,
			decimal lowestAsk,
			decimal highestBid );

		CryptoCoinValue AddCoinValue (
			CryptoCoinId coinId,
			CryptoExchangeId exchangeId,
			decimal lowestAsk,
			decimal highestBid,
			DateTime time );
	}
}