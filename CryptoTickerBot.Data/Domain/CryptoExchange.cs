using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CryptoTickerBot.Data.Enums;
using JetBrains.Annotations;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace CryptoTickerBot.Data.Domain
{
	public class CryptoExchange
	{
		[Key]
		public CryptoExchangeId Id { get; set; }

		[Required]
		public string Name { get; set; }

		[Required]
		[Url]
		public string Url { get; set; }

		[Required]
		public string TickerUrl { get; set; }

		public decimal BuyFees { get; set; }
		public decimal SellFees { get; set; }

		public List<WithdrawalFees> WithdrawalFees { get; set; }
		public List<DepositFees> DepositFees { get; set; }

		public DateTime? LastUpdate { get; set; }
		public DateTime? LastChange { get; set; }
		public virtual List<CryptoCoinValue> CoinValues { get; set; }
		public virtual List<CryptoCoinValue> LatestCoinValues { get; set; }

		public CryptoExchange (
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
		)
		{
			Id         = id;
			Name       = name;
			Url        = url;
			TickerUrl  = tickerUrl;
			BuyFees    = buyFees;
			SellFees   = sellFees;
			LastUpdate = lastUpdate;
			LastChange = lastChange;

			WithdrawalFees = withdrawalFees?
				.Select ( x => new WithdrawalFees ( x.Key, id, x.Value ) )
				.ToList ( );
			DepositFees = depositFees?
				.Select ( x => new DepositFees ( x.Key, id, x.Value ) )
				.ToList ( );
		}

		[UsedImplicitly]
		private CryptoExchange ( )
		{
		}

		public override string ToString ( ) => $"{Name}";
	}
}