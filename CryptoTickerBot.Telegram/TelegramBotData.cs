using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CryptoTickerBot.Collections.Persistent;
using CryptoTickerBot.Collections.Persistent.Base;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Telegram.Subscriptions;
using NLog;
using Telegram.Bot.Types;

namespace CryptoTickerBot.Telegram
{
	public class TelegramBotData
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		public const string FolderName = "Data";

		public PersistentSet<User> Users { get; }
		public PersistentDictionary<int, UserRole> UserRoles { get; }
		public PersistentSet<TelegramPercentChangeSubscription> PercentChangeSubscriptions { get; }

		public User this [ int id ] =>
			Users.FirstOrDefault ( x => x.Id == id );

		public List<User> this [ UserRole role ] =>
			Users
				.Where ( x => UserRoles.TryGetValue ( x.Id, out var r ) && r == role )
				.ToList ( );

		private readonly List<IPersistentCollection> collections;

		public TelegramBotData ( )
		{
			Users = new PersistentSet<User> (
				Path.Combine ( FolderName, "TelegramBotUsers.json" ) );
			UserRoles = new PersistentDictionary<int, UserRole> (
				Path.Combine ( FolderName, "TelegramUserRoles.json" ) );
			PercentChangeSubscriptions =
				new PersistentSet<TelegramPercentChangeSubscription> (
					Path.Combine ( FolderName, "TelegramPercentChangeSubscriptions.json" ) );

			collections = new List<IPersistentCollection>
			{
				Users,
				UserRoles,
				PercentChangeSubscriptions
			};

			foreach ( var collection in collections )
				collection.OnError += OnError;
		}

		public void Save ( )
		{
			foreach ( var collection in collections )
				collection.Save ( );
		}

		public List<TelegramPercentChangeSubscription> GetPercentChangeSubscriptions (
			Func<TelegramPercentChangeSubscription, bool> predicate ) =>
			PercentChangeSubscriptions
				.Where ( predicate )
				.ToList ( );

		public bool AddOrUpdate ( TelegramPercentChangeSubscription subscription ) =>
			PercentChangeSubscriptions.AddOrUpdate ( subscription );

		public bool AddOrUpdate ( User user,
		                          UserRole role )
		{
			var result = Users.AddOrUpdate ( user );
			UserRoles[user.Id] = role;

			return result;
		}

		private void OnError ( IPersistentCollection collection,
		                       Exception exception )
		{
			Logger.Error ( exception, collection.FileName );

			Error?.Invoke ( exception );
		}

		public event ErrorDelegate Error;
	}

	public delegate void ErrorDelegate ( Exception exception );
}