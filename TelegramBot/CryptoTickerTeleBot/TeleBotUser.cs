using System;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace TelegramBot.CryptoTickerTeleBot
{
	[Flags]
	public enum UserRole
	{
		Admin = 1 << 0,
		Registered = 1 << 1,
		Guest = 1 << 2
	}

	public class TeleBotUser : IEquatable<TeleBotUser>
	{
		public UserRole Role { get; set; }
		public string UserName { get; set; }
		public DateTime Created { get; set; } = DateTime.Now;

		public TeleBotUser ( string userName, UserRole role = UserRole.Guest )
		{
			UserName = userName;
			Role     = role;
		}

		public bool Equals ( TeleBotUser other )
		{
			if ( other is null ) return false;
			if ( ReferenceEquals ( this, other ) ) return true;
			return Equals ( UserName, other.UserName );
		}

		public void Register ( ) => Role = UserRole.Guest | UserRole.Registered;

		public void MakeAdmin ( ) => Role = UserRole.Guest | UserRole.Registered | UserRole.Admin;

		public static UserRole GetHighestRole ( UserRole role )
		{
			if ( role.HasFlag ( UserRole.Admin ) )
				return UserRole.Admin;
			if ( role.HasFlag ( UserRole.Registered ) )
				return UserRole.Registered;
			return UserRole.Guest;
		}

		public override string ToString ( ) => $"{GetHighestRole ( Role ),-12} Username: {UserName}";

		public override bool Equals ( object obj )
		{
			if ( obj is null ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;
			if ( obj.GetType ( ) != GetType ( ) ) return false;
			return Equals ( (TeleBotUser) obj );
		}

		public override int GetHashCode ( ) => UserName != null ? UserName.GetHashCode ( ) : 0;

		public static bool operator == ( TeleBotUser left, TeleBotUser right ) => Equals ( left, right );

		public static bool operator != ( TeleBotUser left, TeleBotUser right ) => !Equals ( left, right );
	}
}