using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Repositories;

namespace CryptoTickerBot.Data.Persistence
{
	public class UnitOfWork : IUnitOfWork
	{
		private static readonly object Lock = new object ( );
		private readonly CtbContext context;

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

		[DebuggerStepThrough]
		public int Complete ( ) => context.SaveChanges ( );

		[DebuggerStepThrough]
		public async Task<int> CompleteAsync ( CancellationToken cancellationToken ) =>
			await context.SaveChangesAsync ( cancellationToken );

		public static void Do ( Action<IUnitOfWork> action )
		{
			lock ( Lock )
			{
				using ( var unit = new UnitOfWork ( new CtbContext ( ) ) )
				{
					action ( unit );
					unit.Complete ( );
				}
			}
		}

		public static T Get<T> ( Func<IUnitOfWork, T> func )
		{
			T result;
			lock ( Lock )
			{
				using ( var unit = new UnitOfWork ( new CtbContext ( ) ) )
				{
					result = func ( unit );
					unit.Complete ( );
				}
			}

			return result;
		}

		public static async void DoAsync ( Func<IUnitOfWork, Task> action )
		{
			using ( var unit = new UnitOfWork ( new CtbContext ( ) ) )
			{
				await action ( unit );
				await unit.CompleteAsync ( CancellationToken.None );
			}
		}

		public static async void DoAsync (
			Func<IUnitOfWork, CancellationToken, Task> action,
			CancellationToken cancellationToken )
		{
			using ( var unit = new UnitOfWork ( new CtbContext ( ) ) )
			{
				await action ( unit, cancellationToken );
				await unit.CompleteAsync ( cancellationToken );
			}
		}
	}
}