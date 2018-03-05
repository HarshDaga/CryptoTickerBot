using System;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Repositories;

namespace CryptoTickerBot.Data.Persistence
{
	public interface IUnitOfWork : IDisposable
	{
		ICryptoCoinRepository Coins { get; }
		ICryptoCoinValueRepository CoinValues { get; }
		ICryptoExchangeRepository Exchanges { get; }
		ITeleBotUserRepository Users { get; }
		ITeleSubscriptionRepository Subscriptions { get; }

		int Complete ( );
		Task<int> CompleteAsync ( CancellationToken cancellationToken );
	}
}