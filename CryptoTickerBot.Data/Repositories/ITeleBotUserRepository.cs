using System;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;

namespace CryptoTickerBot.Data.Repositories
{
	public interface ITeleBotUserRepository : IRepository<TeleBotUser>
	{
		void AddOrUpdate ( int id, string userName, UserRole role, DateTime? created = null );
		void Remove ( int id );
	}
}