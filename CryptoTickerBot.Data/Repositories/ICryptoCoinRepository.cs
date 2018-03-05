using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;

namespace CryptoTickerBot.Data.Repositories
{
	public interface ICryptoCoinRepository : IRepository<CryptoCoin>
	{
		CryptoCoin Single ( CryptoCoinId coinId );
		CryptoCoin Single ( string symbol );

		Task<CryptoCoin> SingleAsync ( CryptoCoinId coinId, CancellationToken cancellation );
		Task<CryptoCoin> SingleAsync ( string symbol, CancellationToken cancellationToken );
	}
}