using System;
using CryptoTickerBot.Data.Enums;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace TelegramBot.CryptoTickerTeleBot
{
	public class TeleBotUser : IEquatable<TeleBotUser>
	{
		public UserRole Role { get; }
		public string UserName { get; }
		public DateTime Created { get; }

		public TeleBotUser ( string userName, UserRole role = UserRole.Guest, DateTime? created = null )
		{
			UserName = userName;
			Role     = role;
			Created  = created ?? DateTime.UtcNow;
		}

		public bool Equals ( TeleBotUser other )
		{
			if ( other is null ) return false;
			if ( ReferenceEquals ( this, other ) ) return true;
			return Equals ( UserName, other.UserName );
		}

		public override string ToString ( ) => $"{Role,-12} Username: {UserName}";

		public override bool Equals ( object obj )
		{
			if ( obj is null ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;
			if ( obj.GetType ( ) != GetType ( ) ) return false;
			return Equals ( (TeleBotUser) obj );
		}

		public override int GetHashCode ( ) => UserName?.GetHashCode ( ) ?? 0;

		public static bool operator == ( TeleBotUser left, TeleBotUser right ) => Equals ( left, right );

		public static bool operator != ( TeleBotUser left, TeleBotUser right ) => !Equals ( left, right );
	}
}