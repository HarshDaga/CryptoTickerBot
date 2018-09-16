using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using CryptoTickerBot.Data.Extensions;
using Newtonsoft.Json;

namespace CryptoTickerBot.Data.Domain
{
	public class CryptoCoin : IEquatable<CryptoCoin>
	{
		public Guid Id { get; } = Guid.NewGuid ( );

		public string Symbol { get; }
		public decimal HighestBid { get; set; }
		public decimal LowestAsk { get; set; }
		public decimal Rate { get; set; }
		public DateTime Time { get; set; }

		[JsonIgnore]
		public decimal SellPrice => HighestBid;

		[JsonIgnore]
		public decimal BuyPrice => LowestAsk;

		[JsonIgnore]
		public decimal Average => ( BuyPrice + SellPrice ) / 2;

		[JsonIgnore]
		public decimal Spread => BuyPrice - SellPrice;

		[JsonIgnore]
		public decimal SpreadPercentage => Average != 0 ? Spread / Average : 0;

		public CryptoCoin (
			string symbol,
			decimal highestBid = 0m,
			decimal lowestAsk = 0m,
			decimal rate = 0m,
			DateTime? time = null
		)
		{
			Symbol     = symbol;
			HighestBid = highestBid;
			LowestAsk  = lowestAsk;
			Rate       = rate;
			Time       = time ?? DateTime.UtcNow;
		}

		public virtual bool HasSameValues ( CryptoCoin coin ) =>
			coin != null && Symbol == coin.Symbol &&
			HighestBid == coin.HighestBid && LowestAsk == coin.LowestAsk;

		[DebuggerStepThrough]
		[Pure]
		public CryptoCoin Clone ( ) =>
			new CryptoCoin ( Symbol, HighestBid, LowestAsk, Rate, Time );

		[Pure]
		public override string ToString ( ) =>
			$"{Symbol,-9}: Highest Bid = {HighestBid,-10:N} Lowest Ask = {LowestAsk,-10:N}";

		#region Equality Members

		public bool Equals ( CryptoCoin other )
		{
			if ( other is null ) return false;
			if ( ReferenceEquals ( this, other ) ) return true;
			return string.Equals ( Symbol, other.Symbol, StringComparison.OrdinalIgnoreCase ) &&
			       Time.Equals ( other.Time );
		}

		public override bool Equals ( object obj )
		{
			if ( obj is null ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;
			return obj.GetType ( ) == GetType ( ) && Equals ( (CryptoCoin) obj );
		}

		// ReSharper disable once NonReadonlyMemberInGetHashCode
		public override int GetHashCode ( )
		{
			unchecked
			{
				return ( ( Symbol?.CaseInsensitiveHashCode ( ) ?? 0 ) * 397 ) ^
				       Time.GetHashCode ( );
			}
		}

		public static bool operator == ( CryptoCoin left,
		                                 CryptoCoin right ) => Equals ( left, right );

		public static bool operator != ( CryptoCoin left,
		                                 CryptoCoin right ) => !Equals ( left, right );

		#endregion
	}
}