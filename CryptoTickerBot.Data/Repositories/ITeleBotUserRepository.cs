using System;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;

namespace CryptoTickerBot.Data.Repositories
{
	public interface ITeleBotUserRepository : IRepository<TeleBotUser>
	{
		void Add ( string userName, UserRole role, DateTime? created = null );
		void Remove ( string userName );
	}
}