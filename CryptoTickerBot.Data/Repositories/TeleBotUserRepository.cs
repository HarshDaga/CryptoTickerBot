using System.Data.Entity.Migrations;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Persistence;
using JetBrains.Annotations;

namespace CryptoTickerBot.Data.Repositories
{
	public class TeleBotUserRepository : Repository<TeleBotUser>, ITeleBotUserRepository
	{
		public TeleBotUserRepository ( [NotNull] CtbContext context ) : base ( context )
		{
		}

		public void AddOrUpdate ( [NotNull] TeleBotUser user ) =>
			Context.TeleBotUsers.AddOrUpdate ( user );

		public void UpdateRole ( int id, UserRole role )
		{
			var user = Context.TeleBotUsers.Find ( id );
			if ( user != null )
				user.Role = role;
			Context.SaveChanges ( );
		}

		public void Remove ( int id ) =>
			Remove ( x => x.Id == id );
	}
}