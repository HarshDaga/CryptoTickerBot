using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Repositories;

namespace CryptoTickerBot.Data.Persistence
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly CtbContext context;

		public UnitOfWork ( ) :
			this ( new CtbContext ( ) )
		{
		}

		public UnitOfWork ( CtbContext context )
		{
			this.context  = context;
			Coins         = new CryptoCoinRepository ( context );
			CoinValues    = new CryptoCoinValueRepository ( context );
			Exchanges     = new CryptoExchangeRepository ( context );
			Users         = new TeleBotUserRepository ( context );
			Subscriptions = new TeleSubscriptionRepository ( context );
		}

		public void Dispose ( )
		{
			context.Dispose ( );
		}

		public ICryptoCoinRepository Coins { get; }
		public ICryptoCoinValueRepository CoinValues { get; }
		public ICryptoExchangeRepository Exchanges { get; }
		public ITeleBotUserRepository Users { get; }
		public ITeleSubscriptionRepository Subscriptions { get; }

		public int Complete ( ) => context.SaveChanges ( );

		public async Task<int> CompleteAsync ( CancellationToken cancellationToken ) =>
			await context.SaveChangesAsync ( cancellationToken );
	}
}