using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Collections.Persistent;
using CryptoTickerBot.Domain;
using JetBrains.Annotations;
using Telegram.Bot.Types;

namespace CryptoTickerBot.Telegram
{
	public class TelegramBotData
	{
		public PersistentSet<User> Users { get; [UsedImplicitly] private set; }
		public PersistentDictionary<int, UserRole> UserRoles { get; [UsedImplicitly] private set; }

		public User this [ int id ] =>
			Users.FirstOrDefault ( x => x.Id == id );

		public List<User> this [ UserRole role ] =>
			Users
				.Where ( x => UserRoles.TryGetValue ( x.Id, out var r ) && r == role )
				.ToList ( );

		public TelegramBotData ( )
		{
			Users     = new PersistentSet<User> ( "TelegramBotUsers.json" );
			UserRoles = new PersistentDictionary<int, UserRole> ( "TelegramUserRoles.json" );
			Users.OnError += ( collection,
			                   exception ) => Error?.Invoke ( exception );
			UserRoles.OnError += ( collection,
			                       exception ) => Error?.Invoke ( exception );
		}

		public bool AddUser ( User user,
		                      UserRole role )
		{
			var result = Users.AddOrUpdate ( user );
			UserRoles[user.Id] = role;

			return result;
		}

		public event ErrorDelegate Error;
	}

	public delegate void ErrorDelegate ( Exception exception );
}