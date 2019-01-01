using System;
using CryptoTickerBot.Data.Domain;

namespace CryptoTickerBot.Data.Helpers
{
	public struct PriceChange : IComparable<PriceChange>, IComparable
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

		public int CompareTo ( PriceChange other ) => Value.CompareTo ( other.Value );

		public int CompareTo ( object obj )
		{
			if ( obj is null ) return 1;
			return obj is PriceChange other
				? CompareTo ( other )
				: throw new ArgumentException ( $"Object must be of type {nameof ( PriceChange )}" );
		}

		public static bool operator < ( PriceChange left,
		                                PriceChange right ) =>
			left.CompareTo ( right ) < 0;

		public static bool operator > ( PriceChange left,
		                                PriceChange right ) =>
			left.CompareTo ( right ) > 0;

		public static bool operator <= ( PriceChange left,
		                                 PriceChange right ) =>
			left.CompareTo ( right ) <= 0;

		public static bool operator >= ( PriceChange left,
		                                 PriceChange right ) =>
			left.CompareTo ( right ) >= 0;
	}
}