using System;
using System.Collections.Generic;

namespace CryptoTickerBot
{
	public struct PriceChange
	{
		public decimal Value { get; private set; }
		public decimal Percentage { get; private set; }
		public TimeSpan TimeDiff { get; private set; }
		public DateTime AbsoluteTime { get; private set; }

		public static PriceChange From ( CryptoCoin coin1, CryptoCoin coin2 )
			=> new PriceChange
			{
				Value = coin1.Average - coin2.Average,
				Percentage = ( coin1.Average - coin2.Average ) / ( coin1.Average + coin2.Average ) * 2m,
				TimeDiff = coin1.Time - coin2.Time,
				AbsoluteTime = coin1.Time
			};

		public override string ToString ( ) => $"{Value:N} {Percentage:P}";
	}

	public class CryptoCoin : IEquatable<CryptoCoin>
	{
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
			Symbol = symbol;
			HighestBid = highestBid;
			LowestAsk = lowestAsk;
			Rate = rate;
			Time = DateTime.Now;
		}

		public bool Equals ( CryptoCoin other ) =>
			other != null && Symbol == other.Symbol &&
			HighestBid == other.HighestBid && LowestAsk == other.LowestAsk;

		public virtual decimal Buy ( decimal amountInUsd ) => amountInUsd / BuyPrice;

		public virtual decimal Sell ( decimal quantity ) => SellPrice * quantity;

		public override bool Equals ( object obj ) => Equals ( obj as CryptoCoin );

		public override int GetHashCode ( ) =>
			-1758840423 + EqualityComparer<string>.Default.GetHashCode ( Symbol );

		public CryptoCoin Clone ( ) =>
			new CryptoCoin ( Symbol, HighestBid, LowestAsk, Rate );

		public static bool operator == ( CryptoCoin coin1, CryptoCoin coin2 ) =>
			EqualityComparer<CryptoCoin>.Default.Equals ( coin1, coin2 );

		public static bool operator != ( CryptoCoin coin1, CryptoCoin coin2 ) =>
			!( coin1 == coin2 );

		public static PriceChange operator - ( CryptoCoin coin1, CryptoCoin coin2 ) =>
			PriceChange.From ( coin1, coin2 );

		public override string ToString ( ) =>
			$"{Symbol}: Highest Bid = {HighestBid,-10:C} Lowest Ask = {LowestAsk,-10:C}";

		public IList<object> ToSheetsRow ( ) =>
			new List<object> {Symbol, LowestAsk, HighestBid, Rate};
	}
}