using System;
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
		public static bool Contains ( this IEnumerable<TeleBotUser> users, string userName ) =>
			users.Any ( x => x.UserName.Equals (
				            userName,
				            StringComparison.InvariantCultureIgnoreCase
			            ) );

		[DebuggerStepThrough]
		public static TeleBotUser Get ( this IEnumerable<TeleBotUser> users, string userName ) =>
			users.FirstOrDefault ( x => x.UserName.Equals (
				                       userName,
				                       StringComparison.InvariantCultureIgnoreCase
			                       ) );

		public static TeleBotUser AddOrUpdate (
			this IList<TeleBotUser> users,
			TeleBotUser user
		)
		{
			if ( users.Contains ( user.UserName ) )
				users.Remove ( users.Get ( user.UserName ) );

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