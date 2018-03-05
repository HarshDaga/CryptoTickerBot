using System;

namespace CryptoTickerBot
{
	public struct PriceChange
	{
		public decimal Value { get; private set; }
		public decimal Percentage { get; private set; }
		public TimeSpan TimeDiff { get; private set; }
		public DateTime AbsoluteTime { get; private set; }

		public static PriceChange From ( CryptoCoin newCoin, CryptoCoin oldCoin )
			=> new PriceChange
			{
				Value = newCoin.Average - oldCoin.Average,
				Percentage = ( newCoin.Average - oldCoin.Average ) /
				             ( newCoin.Average + oldCoin.Average ) *
				             2m,
				TimeDiff     = newCoin.Time - oldCoin.Time,
				AbsoluteTime = newCoin.Time
			};

		public override string ToString ( ) => $"{Value:N} {Percentage:P}";
	}
}