using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Persistence;
using JetBrains.Annotations;

namespace CryptoTickerBot.Data.Repositories
{
	public class CryptoExchangeRepository : Repository<CryptoExchange>, ICryptoExchangeRepository
	{
		protected override IQueryable<CryptoExchange> AllEntities =>
			Context.Exchanges
				.Include ( x => x.CoinValues )
				.Include ( x => x.CoinValues.Select ( v => v.Coin ) )
				.Include ( x => x.WithdrawalFees )
				.Include ( x => x.WithdrawalFees.Select ( f => f.Coin ) )
				.Include ( x => x.DepositFees )
				.Include ( x => x.WithdrawalFees.Select ( f => f.Coin ) );

		public CryptoExchangeRepository ( CtbContext context ) : base ( context )
		{
		}

		[Pure]
		public CryptoExchange Get ( CryptoExchangeId id ) =>
			Context.Exchanges
				.Include ( x => x.WithdrawalFees )
				.Include ( x => x.WithdrawalFees.Select ( f => f.Coin ) )
				.Include ( x => x.DepositFees )
				.Include ( x => x.WithdrawalFees.Select ( f => f.Coin ) )
				.FirstOrDefault ( x => x.Id == id );

		public void AddExchange (
			CryptoExchangeId id,
			string name,
			string url,
			string tickerUrl,
			decimal buyFees = 0,
			decimal sellFees = 0,
			DateTime? lastUpdate = null,
			DateTime? lastChange = null,
			IDictionary<CryptoCoinId, decimal> withdrawalFees = null,
			IDictionary<CryptoCoinId, decimal> depositFees = null
		) =>
			Context.Exchanges.Add ( new CryptoExchange ( id, name, url, tickerUrl,
			                                             buyFees, sellFees, lastUpdate, lastChange,
			                                             withdrawalFees, depositFees )
			);

		public bool UpdateExchange (
			CryptoExchangeId id,
			string name = null,
			string url = null,
			string tickerUrl = null,
			decimal buyFees = -1,
			decimal sellFees = -1,
			DateTime? lastUpdate = null,
			DateTime? lastChange = null,
			IDictionary<CryptoCoinId, decimal> withdrawalFees = null,
			IDictionary<CryptoCoinId, decimal> depositFees = null
		)
		{
			var exchange = Context.Exchanges.FirstOrDefault ( x => x.Id == id );

			if ( exchange == null ) return false;

			if ( name != null )
				exchange.Name = name;
			if ( url != null )
				exchange.Url = url;
			if ( tickerUrl != null )
				exchange.TickerUrl = tickerUrl;
			if ( buyFees != -1 )
				exchange.BuyFees = buyFees;
			if ( sellFees != -1 )
				exchange.SellFees = sellFees;
			if ( lastUpdate != null )
				exchange.LastUpdate = lastUpdate;
			if ( lastChange != null )
				exchange.LastChange = lastChange;
			if ( withdrawalFees != null )
				exchange.WithdrawalFees = withdrawalFees
					.Select ( x => new WithdrawalFees ( x.Key, id, x.Value ) )
					.ToList ( );
			if ( depositFees != null )
				exchange.DepositFees = depositFees
					.Select ( x => new DepositFees ( x.Key, id, x.Value ) )
					.ToList ( );

			return true;
		}
	}
}