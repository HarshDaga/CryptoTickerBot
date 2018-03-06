using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CryptoTickerBot.Data.Enums;
using JetBrains.Annotations;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

// ReSharper disable UnusedMember.Local

namespace CryptoTickerBot.Data.Domain
{
	public class WithdrawalFees
	{
		[Key]
		[Column ( Order = 1 )]
		[ForeignKey ( nameof ( Exchange ) )]
		public CryptoExchangeId ExchangeId { get; set; }

		[Key]
		[Column ( Order = 2 )]
		[ForeignKey ( nameof ( Coin ) )]
		public CryptoCoinId CoinId { get; set; }

		[Required]
		public decimal Value { get; set; }

		public CryptoExchange Exchange { get; set; }
		public CryptoCoin Coin { get; set; }

		public WithdrawalFees ( CryptoCoinId coinId, CryptoExchangeId exchangeId, decimal value )
		{
			CoinId     = coinId;
			ExchangeId = exchangeId;
			Value      = value;
		}

		[UsedImplicitly]
		private WithdrawalFees ( )
		{
		}
	}
}