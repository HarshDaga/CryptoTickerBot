using System;
using System.Collections.Generic;

namespace CryptoTickerBot
{
	public class CryptoCoin : IEquatable<CryptoCoin>
	{
		public CryptoCoin ( string symbol, decimal highestBid = 0m, decimal lowestAsk = 0m, decimal rate = 0m )
		{
			Symbol = symbol;
			HighestBid = highestBid;
			LowestAsk = lowestAsk;
			Rate = rate;
		}

		public string Symbol { get; }
		public decimal HighestBid { get; set; }
		public decimal LowestAsk { get; set; }
		public decimal Rate { get; set; }

		public virtual decimal Buy ( decimal amountInUsd ) => amountInUsd / HighestBid;

		public virtual decimal Sell ( decimal quantity ) => LowestAsk * quantity;

		public override bool Equals ( object obj ) => Equals ( obj as CryptoCoin );

		public bool Equals ( CryptoCoin other ) => other != null && Symbol == other.Symbol &&
		                                           HighestBid == other.HighestBid && LowestAsk == other.LowestAsk;

		public override int GetHashCode ( ) => -1758840423 + EqualityComparer<string>.Default.GetHashCode ( Symbol );

		public CryptoCoin Clone ( ) => new CryptoCoin ( Symbol, HighestBid, LowestAsk, Rate );

		public static bool operator == ( CryptoCoin coin1, CryptoCoin coin2 ) =>
			EqualityComparer<CryptoCoin>.Default.Equals ( coin1, coin2 );

		public static bool operator != ( CryptoCoin coin1, CryptoCoin coin2 ) => !( coin1 == coin2 );

		public override string ToString ( ) => $"{Symbol}: Highest Bid = {HighestBid,-10:C} Lowest Ask = {LowestAsk,-10:C}";

		public IList<object> ToSheetsRow ( ) =>
			new List<object> {Symbol, LowestAsk, HighestBid, Rate};
	}
}