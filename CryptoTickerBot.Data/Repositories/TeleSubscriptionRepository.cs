using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Persistence;
using JetBrains.Annotations;

namespace CryptoTickerBot.Data.Repositories
{
	public class TeleSubscriptionRepository : Repository<TeleSubscription>, ITeleSubscriptionRepository
	{
		protected override IQueryable<TeleSubscription> AllEntities =>
			Context.TeleSubscriptions
				.Include ( x => x.Coins )
				.Include ( x => x.Exchange );

		public TeleSubscriptionRepository ( [NotNull] CtbContext context ) : base ( context )
		{
		}

		public TeleSubscription Add (
			CryptoExchangeId exchangeId,
			long chatId,
			string userName,
			decimal threshold,
			IEnumerable<CryptoCoinId> coinIds,
			IDictionary<CryptoCoinId, CryptoCoinValue> lastSignificantPrice = null,
			DateTime? startDate = null,
			DateTime? endDate = null
		)
		{
			var coins = Context.Coins.ToList ( ).Where ( x => coinIds.Contains ( x.Id ) );

			var sub = new TeleSubscription (
				exchangeId, chatId, userName, threshold,
				coins, lastSignificantPrice,
				startDate, endDate
			);
			Add ( sub );

			return sub;
		}

		public IEnumerable<TeleSubscription> GetAll ( CryptoExchangeId exchangeId ) =>
			AllEntities.Where ( x => x.ExchangeId == exchangeId ).ToList ( );

		public IEnumerable<TeleSubscription> GetAll ( long chatId ) =>
			AllEntities.Where ( x => x.ChatId == chatId ).ToList ( );

		public void Remove ( long chatId ) =>
			Remove ( s => s.ChatId == chatId );

		public void UpdateCoin ( TeleSubscription subscription, CryptoCoinId coinId )
		{
			var ccv = Context.CoinValues
				.OrderByDescending ( x => x.Id )
				.FirstOrDefault (
					x =>
						x.ExchangeId == subscription.ExchangeId &&
						x.CoinId == coinId
				);
			if ( ccv != null )
			{
				subscription.LastSignificantPrice[coinId] = ccv;
				Context.TeleSubscriptions.AddOrUpdate ( subscription );
			}
		}

		public void SetEndDate ( int subscriptionId, DateTime? endDate )
		{
			var subscription = Get ( subscriptionId );
			subscription.EndDate = endDate;
			Context.TeleSubscriptions.AddOrUpdate ( subscription );
		}

		public void SetExpired ( int subscriptionId )
		{
			var subscription = Get ( subscriptionId );
			subscription.Expired = true;
			Context.TeleSubscriptions.AddOrUpdate ( subscription );
		}
	}
}