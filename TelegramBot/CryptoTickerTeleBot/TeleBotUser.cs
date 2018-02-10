using System;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace TelegramBot.CryptoTickerTeleBot
{
	[Flags]
	public enum UserRole : uint
	{
		Admin = 1 << 0,
		Registered = 1 << 1,
		Guest = 1 << 2,
		None = 1 << 20
	}

	public class TeleBotUser : IEquatable<TeleBotUser>
	{
		public const UserRole Guest = UserRole.Guest;
		public const UserRole Registered = Guest | UserRole.Registered;
		public const UserRole Admin = Registered | UserRole.Admin;
		private static readonly UserRole[] RolePriority = {UserRole.Admin, UserRole.Registered, UserRole.Guest};

		public UserRole Role { get; set; }
		public string UserName { get; }
		public DateTime Created { get; set; } = DateTime.Now;

		public TeleBotUser ( string userName, UserRole role = Guest )
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

		public void Register ( ) => Role = Registered;

		public void MakeAdmin ( ) => Role = Admin;

		public static UserRole GetHighestRole ( UserRole role )
		{
			foreach ( var r in RolePriority )
				if ( role.HasFlag ( r ) )
					return r;
			return UserRole.None;
		}

		public override string ToString ( ) => $"{GetHighestRole ( Role ),-12} Username: {UserName}";

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