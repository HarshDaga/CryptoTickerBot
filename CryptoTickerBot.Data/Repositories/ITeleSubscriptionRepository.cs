using System;
using System.Collections.Generic;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;

namespace CryptoTickerBot.Data.Repositories
{
	public interface ITeleSubscriptionRepository : IRepository<TeleSubscription>
	{
		TeleSubscription Add (
			CryptoExchangeId exchangeId,
			long chatId,
			string userName,
			decimal threshold,
			IEnumerable<CryptoCoinId> coinIds,
			IDictionary<CryptoCoinId, CryptoCoinValue> lastSignificantPrice = null,
			DateTime? startDate = null,
			DateTime? endDate = null
		);

		IEnumerable<TeleSubscription> GetAll ( CryptoExchangeId exchangeId );

		IEnumerable<TeleSubscription> GetAll ( long chatId );

		void Remove ( long chatId );

		TeleSubscription UpdateCoin ( int subscriptionId, CryptoCoinValue ccv );

		void SetEndDate ( int subscriptionId, DateTime? endDate = null );

		void SetExpired ( int subscriptionId );
	}
}