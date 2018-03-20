using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable UnusedMemberInSuper.Global

namespace CryptoTickerBot.Data.Repositories
{
	public interface IRepository<TEntity> where TEntity : class
	{
		TEntity Get ( params object[] id );
		IEnumerable<TEntity> GetAll ( );
		IEnumerable<TEntity> Find ( Expression<Func<TEntity, bool>> predicate );

		Task<TEntity> GetAsync ( object id, CancellationToken cancellationToken );
		Task<IEnumerable<TEntity>> GetAllAsync ( CancellationToken cancellationToken );

		Task<IEnumerable<TEntity>> FindAsync (
			Expression<Func<TEntity, bool>> predicate,
			CancellationToken cancellationToken );

		TEntity Add ( TEntity ccv );
		IEnumerable<TEntity> AddRange ( IEnumerable<TEntity> entities );

		TEntity Remove ( TEntity entity );
		void Remove ( Expression<Func<TEntity, bool>> predicate );

		IEnumerable<TEntity> RemoveRange ( IEnumerable<TEntity> entities );
	}
}