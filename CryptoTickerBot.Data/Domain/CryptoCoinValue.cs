using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CryptoTickerBot.Data.Enums;
using JetBrains.Annotations;
using Newtonsoft.Json;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace CryptoTickerBot.Data.Domain
{
	public class CryptoCoinValue
	{
		[Key]
		[DatabaseGenerated ( DatabaseGeneratedOption.Identity )]
		public int Id { get; set; }

		[ForeignKey ( nameof ( Coin ) )]
		public CryptoCoinId CoinId { get; set; }

		[ForeignKey ( nameof ( Exchange ) )]
		public CryptoExchangeId ExchangeId { get; set; }

		[JsonIgnore]
		public CryptoCoin Coin { get; set; }

		[JsonIgnore]
		public CryptoExchange Exchange { get; set; }

		[Required]
		public decimal LowestAsk { get; set; }

		[Required]
		public decimal HighestBid { get; set; }

		[Index]
		[DatabaseGenerated ( DatabaseGeneratedOption.None )]
		public DateTime Time { get; set; }

		public CryptoCoinValue (
			CryptoCoinId coinId,
			CryptoExchangeId exchangeId,
			decimal lowestAsk,
			decimal highestBid
		) :
			this ( coinId, exchangeId, lowestAsk, highestBid, DateTime.UtcNow )
		{
		}

		public CryptoCoinValue (
			CryptoCoinId coinId,
			CryptoExchangeId exchangeId,
			decimal lowestAsk,
			decimal highestBid,
			DateTime time )
		{
			CoinId     = coinId;
			ExchangeId = exchangeId;
			LowestAsk  = lowestAsk;
			HighestBid = highestBid;
			Time       = time;
		}

		[UsedImplicitly]
		private CryptoCoinValue ( )
		{
		}

		public override string ToString ( ) =>
			$"{Coin?.Symbol}: Highest Bid = {HighestBid,-10:C} Lowest Ask = {LowestAsk,-10:C}";
	}
}