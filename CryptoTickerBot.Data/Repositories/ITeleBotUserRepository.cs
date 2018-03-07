using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;

namespace CryptoTickerBot.Data.Repositories
{
	public interface ITeleBotUserRepository : IRepository<TeleBotUser>
	{
		void AddOrUpdate ( TeleBotUser user );
		void UpdateRole ( int id, UserRole role );
		void Remove ( int id );
	}
}