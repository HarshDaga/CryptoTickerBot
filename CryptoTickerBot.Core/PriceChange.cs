using System;

namespace CryptoTickerBot.Core
{
	public struct PriceChange
	{
		public decimal Value { get; private set; }
		public decimal Percentage { get; private set; }
		public TimeSpan TimeDiff { get; private set; }
		public DateTime AbsoluteTime { get; private set; }

		public static PriceChange Difference ( CryptoCoin newCoin,
		                                       CryptoCoin oldCoin )
			=> new PriceChange
			{
				Value = newCoin.Rate - oldCoin.Rate,
				Percentage = ( newCoin.Rate - oldCoin.Rate ) /
				             ( newCoin.Rate + oldCoin.Rate ) *
				             2m,
				TimeDiff     = newCoin.Time - oldCoin.Time,
				AbsoluteTime = newCoin.Time
			};

		public override string ToString ( ) => $"{Value:N} {Percentage:P}";
	}
}