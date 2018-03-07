using System;
using System.Diagnostics.Contracts;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;
using Telegram.Bot.Types;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace TelegramBot
{
	public class TelegramBotUser : IEquatable<TelegramBotUser>
	{
		public int Id { get; }
		public UserRole Role { get; set; }
		public string UserName { get; }
		public string FirstName { get; }
		public string LastName { get; }
		public DateTime Created { get; }

		public TelegramBotUser (
			int id,
			UserRole role = UserRole.Guest,
			string userName = null,
			string firstName = null,
			string lastName = null,
			DateTime? created = null
		)
		{
			Id        = id;
			UserName  = userName;
			FirstName = firstName;
			LastName  = lastName;
			Role      = role;
			Created   = created ?? DateTime.UtcNow;
		}

		public TelegramBotUser ( User user, UserRole role = UserRole.Guest,
		                         DateTime? created = null )
			: this ( user.Id, role, user.Username, user.FirstName, user.LastName, created )
		{
		}

		public bool Equals ( TelegramBotUser other )
		{
			if ( other is null ) return false;
			if ( ReferenceEquals ( this, other ) ) return true;
			return Id == other.Id;
		}

		public static implicit operator TelegramBotUser ( User user ) =>
			new TelegramBotUser ( user.Id, UserRole.Guest, user.Username, user.FirstName, user.LastName );

		public static implicit operator TelegramBotUser ( TeleBotUser user ) =>
			new TelegramBotUser ( user.Id, user.Role, user.UserName, user.FirstName, user.LastName, user.Created );

		public static implicit operator TeleBotUser ( TelegramBotUser user ) =>
			new TeleBotUser ( user.Id, user.Role, user.UserName, user.FirstName, user.LastName, user.Created );

		[Pure]
		public override string ToString ( ) =>
			$"{Id,-10} {Role,-12} {FirstName} {LastName} {Created:g}";

		public override bool Equals ( object obj )
		{
			if ( obj is null ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;
			if ( obj.GetType ( ) != GetType ( ) ) return false;
			return Equals ( (TelegramBotUser) obj );
		}

		public override int GetHashCode ( ) => UserName?.GetHashCode ( ) ?? 0;

		public static bool operator == ( TelegramBotUser left, TelegramBotUser right ) => Equals ( left, right );

		public static bool operator != ( TelegramBotUser left, TelegramBotUser right ) => !Equals ( left, right );
	}
}