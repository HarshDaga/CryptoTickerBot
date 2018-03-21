using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Persistence;
using JetBrains.Annotations;

namespace CryptoTickerBot.Data.Repositories
{
	public class CryptoCoinRepository : Repository<CryptoCoin>, ICryptoCoinRepository
	{
		public CtbContext CtbContext => Context;

		public CryptoCoinRepository ( CtbContext context ) : base ( context )
		{
		}

		[Pure]
		public CryptoCoin Single ( CryptoCoinId coinId ) =>
			CtbContext.Coins.Single ( c => c.Id == coinId );

		[Pure]
		public CryptoCoin Single ( string symbol ) =>
			CtbContext.Coins.Single ( c => c.Symbol == symbol );

		public async Task<CryptoCoin> SingleAsync (
			CryptoCoinId coinId,
			CancellationToken cancellation
		) =>
			await CtbContext.Coins.SingleAsync ( c => c.Id == coinId, cancellation ).ConfigureAwait ( false );

		public async Task<CryptoCoin> SingleAsync (
			string symbol,
			CancellationToken cancellation
		) =>
			await CtbContext.Coins.SingleAsync ( c => c.Symbol == symbol, cancellation ).ConfigureAwait ( false );
	}
}