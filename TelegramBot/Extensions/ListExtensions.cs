using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CryptoTickerBot.Data.Enums;

namespace TelegramBot.Extensions
{
	public static class ListExtensions
	{
		[DebuggerStepThrough]
		public static bool Contains ( this IEnumerable<TelegramBotUser> users, int id ) =>
			users.Any ( x => x.Id == id );

		[DebuggerStepThrough]
		public static TelegramBotUser Get ( this IEnumerable<TelegramBotUser> users, int id ) =>
			users.FirstOrDefault ( x => x.Id == id );

		public static TelegramBotUser AddOrUpdate (
			this IList<TelegramBotUser> users,
			TelegramBotUser user
		)
		{
			if ( users.Contains ( user.Id ) )
				users.Remove ( users.Get ( user.Id ) );

			users.Add ( user );

			return user;
		}

		[DebuggerStepThrough]
		public static IEnumerable<TelegramBotUser> OfRole (
			this IEnumerable<TelegramBotUser> users,
			UserRole role
		) =>
			users.Where ( x => x.Role == role );
	}
}