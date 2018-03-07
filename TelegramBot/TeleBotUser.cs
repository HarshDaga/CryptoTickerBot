using System;
using System.Diagnostics.Contracts;
using CryptoTickerBot.Data.Enums;
using Telegram.Bot.Types;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace TelegramBot
{
	public class TeleBotUser : IEquatable<TeleBotUser>
	{
		public int Id { get; }
		public UserRole Role { get; }
		public string UserName { get; }
		public DateTime Created { get; }

		public TeleBotUser (
			int id,
			string userName,
			UserRole role = UserRole.Guest,
			DateTime? created = null
		)
		{
			Id       = id;
			UserName = userName;
			Role     = role;
			Created  = created ?? DateTime.UtcNow;
		}

		public TeleBotUser ( User user, UserRole role = UserRole.Guest, DateTime? created = null )
			: this ( user.Id, user.Username, role, created )
		{
		}

		public bool Equals ( TeleBotUser other )
		{
			if ( other is null ) return false;
			if ( ReferenceEquals ( this, other ) ) return true;
			return Id == other.Id;
		}

		[Pure]
		public override string ToString ( ) => $"{Id,-10} {Role,-12} Username: {UserName}";

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