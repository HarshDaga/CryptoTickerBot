using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Persistence;

namespace CryptoTickerBot.Data.Repositories
{
	public class CryptoCoinValueRepository : Repository<CryptoCoinValue>, ICryptoCoinValueRepository
	{
		protected override IQueryable<CryptoCoinValue> AllEntities =>
			Context.CoinValues
				.Include ( x => x.Coin )
				.Include ( x => x.Exchange );

		public CryptoCoinValueRepository ( CtbContext context ) : base ( context )
		{
		}

		public override async Task<IEnumerable<CryptoCoinValue>> GetAllAsync ( CancellationToken cancellationToken ) =>
			await AllEntities
				.ToListAsync ( cancellationToken )
				.ConfigureAwait ( false );

		public override async Task<IEnumerable<CryptoCoinValue>> FindAsync (
			Expression<Func<CryptoCoinValue, bool>> predicate, CancellationToken cancellationToken ) =>
			await AllEntities
				.Where ( predicate )
				.ToListAsync ( cancellationToken )
				.ConfigureAwait ( false );

		public async Task<IEnumerable<CryptoCoinValue>> GetAllAsync (
			CryptoCoinId coinId,
			CancellationToken cancellationToken ) =>
			await AllEntities
				.Where ( x => x.CoinId == coinId )
				.ToListAsync ( cancellationToken )
				.ConfigureAwait ( false );

		public async Task<IEnumerable<CryptoCoinValue>> GetAllAsync (
			CryptoExchangeId exchangeId,
			CancellationToken cancellationToken ) =>
			await AllEntities
				.Where ( x => x.ExchangeId == exchangeId )
				.ToListAsync ( cancellationToken )
				.ConfigureAwait ( false );

		public async Task<IEnumerable<CryptoCoinValue>> GetAllAsync (
			CryptoCoinId coinId,
			CryptoExchangeId exchangeId,
			CancellationToken cancellationToken ) =>
			await AllEntities
				.Where ( x => x.CoinId == coinId && x.ExchangeId == exchangeId )
				.ToListAsync ( cancellationToken )
				.ConfigureAwait ( false );

		public async Task<IEnumerable<CryptoCoinValue>> GetAllAsync (
			int count,
			CancellationToken cancellationToken ) =>
			await AllEntities
				.OrderByDescending ( x => x.Time )
				.Take ( count )
				.ToListAsync ( cancellationToken )
				.ConfigureAwait ( false );

		public async Task<IEnumerable<CryptoCoinValue>> GetLastAsync (
			CryptoCoinId coinId,
			int count,
			CancellationToken cancellationToken ) =>
			await AllEntities
				.Where ( x => x.CoinId == coinId )
				.OrderByDescending ( x => x.Time )
				.Take ( count )
				.ToListAsync ( cancellationToken )
				.ConfigureAwait ( false );

		public async Task<IEnumerable<CryptoCoinValue>> GetLastAsync (
			CryptoExchangeId exchangeId,
			int count,
			CancellationToken cancellationToken ) =>
			await AllEntities
				.Where ( x => x.ExchangeId == exchangeId )
				.OrderByDescending ( x => x.Time )
				.Take ( count )
				.ToListAsync ( cancellationToken )
				.ConfigureAwait ( false );

		public async Task<IEnumerable<CryptoCoinValue>> GetLastAsync (
			CryptoCoinId coinId,
			CryptoExchangeId exchangeId,
			int count, CancellationToken cancellationToken ) =>
			await AllEntities
				.Where ( x => x.CoinId == coinId && x.ExchangeId == exchangeId )
				.OrderByDescending ( x => x.Time )
				.Take ( count )
				.ToListAsync ( cancellationToken )
				.ConfigureAwait ( false );

		public CryptoCoinValue AddCoinValue (
			CryptoCoinId coinId,
			CryptoExchangeId exchangeId,
			decimal lowestAsk,
			decimal highestBid ) =>
			AddCoinValue ( coinId, exchangeId, lowestAsk, highestBid, DateTime.UtcNow );

		public CryptoCoinValue AddCoinValue (
			CryptoCoinId coinId,
			CryptoExchangeId exchangeId,
			decimal lowestAsk,
			decimal highestBid,
			DateTime time ) =>
			Add ( new CryptoCoinValue (
				      coinId,
				      exchangeId,
				      lowestAsk,
				      highestBid,
				      time )
			);

		public override CryptoCoinValue Add ( CryptoCoinValue ccv )
		{
			var result = Context.CoinValues.Add ( ccv );
			Context.SaveChanges ( );

			var exchange = Context.Exchanges
				.Include ( x => x.LatestCoinValues )
				.FirstOrDefault ( x => x.Id == ccv.ExchangeId );

			if ( exchange != null )
			{
				exchange.LatestCoinValues.RemoveAll ( c => c.CoinId == ccv.CoinId );
				exchange.LatestCoinValues.Add ( result );
				exchange.LastChange = ccv.Time;
			}

			return result;
		}
	}
}