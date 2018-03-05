﻿using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Data.Enums;
using TelegramBot.CryptoTickerTeleBot;

namespace TelegramBot.Extensions
{
	public static class ListExtensions
	{
		public static bool Contains ( this IEnumerable<TeleBotUser> users, string userName ) =>
			users.Any ( x => x.UserName.Equals (
				            userName,
				            StringComparison.InvariantCultureIgnoreCase
			            ) );

		public static TeleBotUser Get ( this IEnumerable<TeleBotUser> users, string userName ) =>
			users.FirstOrDefault ( x => x.UserName.Equals (
				                       userName,
				                       StringComparison.InvariantCultureIgnoreCase
			                       ) );

		public static IEnumerable<TeleBotUser> OfRole (
			this IEnumerable<TeleBotUser> users,
			UserRole role
		) =>
			users.Where ( x => x.Role == role );
	}
}