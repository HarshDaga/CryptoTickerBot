using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CryptoTickerBot.Data.Enums;
using TelegramBot.CryptoTickerTeleBot;

namespace TelegramBot.Extensions
{
	public static class ListExtensions
	{
		[DebuggerStepThrough]
		public static bool Contains ( this IEnumerable<TeleBotUser> users, int id ) =>
			users.Any ( x => x.Id == id );

		[DebuggerStepThrough]
		public static TeleBotUser Get ( this IEnumerable<TeleBotUser> users, int id ) =>
			users.FirstOrDefault ( x => x.Id == id );

		public static TeleBotUser AddOrUpdate (
			this IList<TeleBotUser> users,
			TeleBotUser user
		)
		{
			if ( users.Contains ( user.Id ) )
				users.Remove ( users.Get ( user.Id ) );

			users.Add ( user );

			return user;
		}

		[DebuggerStepThrough]
		public static IEnumerable<TeleBotUser> OfRole (
			this IEnumerable<TeleBotUser> users,
			UserRole role
		) =>
			users.Where ( x => x.Role == role );
	}
}