using System;
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

		public void Add ( string userName, UserRole role, DateTime? created = null ) =>
			Add ( new TeleBotUser ( userName, role, created ) );

		public void Remove ( string userName ) =>
			Remove ( x => x.UserName.Equals ( userName, StringComparison.InvariantCultureIgnoreCase ) );
	}
}