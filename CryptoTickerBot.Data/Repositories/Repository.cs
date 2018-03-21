using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Persistence;
using JetBrains.Annotations;

namespace CryptoTickerBot.Data.Repositories
{
	public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
	{
		protected readonly CtbContext Context;

		protected virtual IQueryable<TEntity> AllEntities { get; }

		public Repository ( [NotNull] CtbContext context )
		{
			Context     = context;
			AllEntities = Context.Set<TEntity> ( );
		}

		[Pure]
		public virtual TEntity Get ( params object[] id ) =>
			Context.Set<TEntity> ( ).Find ( id );

		[Pure]
		public virtual IEnumerable<TEntity> GetAll ( ) =>
			AllEntities.ToList ( );

		[Pure]
		public virtual IEnumerable<TEntity> Find ( Expression<Func<TEntity, bool>> predicate ) =>
			AllEntities.Where ( predicate );

		public virtual async Task<TEntity> GetAsync ( object id, CancellationToken cancellationToken ) =>
			await Context.Set<TEntity> ( ).FindAsync ( cancellationToken, id ).ConfigureAwait ( false );

		public virtual async Task<IEnumerable<TEntity>> GetAllAsync ( CancellationToken cancellationToken ) =>
			await AllEntities.ToListAsync ( cancellationToken ).ConfigureAwait ( false );

		public virtual async Task<IEnumerable<TEntity>> FindAsync (
			Expression<Func<TEntity, bool>> predicate,
			CancellationToken cancellationToken ) =>
			await AllEntities.Where ( predicate ).ToListAsync ( cancellationToken ).ConfigureAwait ( false );

		public virtual TEntity Add ( TEntity ccv ) =>
			Context.Set<TEntity> ( ).Add ( ccv );

		public virtual IEnumerable<TEntity> AddRange ( IEnumerable<TEntity> entities ) =>
			Context.Set<TEntity> ( ).AddRange ( entities );

		public virtual TEntity Remove ( TEntity entity ) =>
			Context.Set<TEntity> ( ).Remove ( entity );

		public virtual void Remove ( Expression<Func<TEntity, bool>> predicate ) =>
			RemoveRange ( Context.Set<TEntity> ( ).Where ( predicate ) );

		public virtual IEnumerable<TEntity> RemoveRange ( IEnumerable<TEntity> entities ) =>
			Context.Set<TEntity> ( ).RemoveRange ( entities );
	}
}