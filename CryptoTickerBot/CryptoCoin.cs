using System;
using System.Collections.Generic;
using System.Diagnostics;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;

namespace CryptoTickerBot
{
	public class CryptoCoin : IEquatable<CryptoCoin>
	{
		public CryptoCoinId Id { get; }
		public string Symbol { get; }
		public decimal HighestBid { get; set; }
		public decimal LowestAsk { get; set; }
		public decimal SellPrice => HighestBid;
		public decimal BuyPrice => LowestAsk;
		public decimal Average => ( BuyPrice + SellPrice ) / 2;
		public decimal Rate { get; set; }
		public decimal Spread => BuyPrice - SellPrice;
		public decimal SpreadPercentange => Spread / ( BuyPrice + SellPrice ) * 2;
		public DateTime Time { get; }

		public CryptoCoin (
			string symbol,
			decimal highestBid = 0m, decimal lowestAsk = 0m,
			decimal rate = 0m
		)
		{
			if ( Enum.TryParse<CryptoCoinId> ( symbol, out var id ) )
				Id = id;
			Symbol     = symbol;
			HighestBid = highestBid;
			LowestAsk  = lowestAsk;
			Rate       = rate;
			Time       = DateTime.UtcNow;
		}

		public CryptoCoin ( CryptoCoinValue coinValue )
		{
			Id         = coinValue.CoinId;
			Symbol     = coinValue.CoinId.ToString ( );
			HighestBid = coinValue.HighestBid;
			LowestAsk  = coinValue.LowestAsk;
			Time       = coinValue.Time;
			Rate       = ( HighestBid + LowestAsk ) / 2m;
		}

		public bool Equals ( CryptoCoin other ) =>
			other != null && Symbol == other.Symbol &&
			HighestBid == other.HighestBid && LowestAsk == other.LowestAsk;

		[DebuggerStepThrough]
		public virtual decimal Buy ( decimal amountInUsd ) => amountInUsd / BuyPrice;

		[DebuggerStepThrough]
		public virtual decimal Sell ( decimal quantity ) => SellPrice * quantity;

		public override bool Equals ( object obj ) => Equals ( obj as CryptoCoin );

		public override int GetHashCode ( ) =>
			-1758840423 + EqualityComparer<string>.Default.GetHashCode ( Symbol );

		[DebuggerStepThrough]
		public CryptoCoin Clone ( ) =>
			new CryptoCoin ( Symbol, HighestBid, LowestAsk, Rate );

		[DebuggerStepThrough]
		public static bool operator == ( CryptoCoin coin1, CryptoCoin coin2 ) =>
			EqualityComparer<CryptoCoin>.Default.Equals ( coin1, coin2 );

		[DebuggerStepThrough]
		public static bool operator != ( CryptoCoin coin1, CryptoCoin coin2 ) =>
			!( coin1 == coin2 );

		public static PriceChange operator - ( CryptoCoin coin1, CryptoCoin coin2 ) =>
			PriceChange.From ( coin1, coin2 );

		public override string ToString ( ) =>
			$"{Symbol}: Highest Bid = {HighestBid,-10:C} Lowest Ask = {LowestAsk,-10:C}";

		public IList<object> ToSheetsRow ( ) =>
			new List<object> {Symbol, LowestAsk, HighestBid, $"{Time:G}"};
	}
}