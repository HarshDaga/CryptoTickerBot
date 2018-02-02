using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;

namespace TelegramBot.CryptoTickerTeleBot
{
	public class TeleBotUserList : IList<TeleBotUser>
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private readonly object fileLock = new object ( );
		private readonly string fileName;
		private readonly object listLock = new object ( );
		private IList<TeleBotUser> listImplementation;

		public TeleBotUser this [ string userName ] =>
			listImplementation.FirstOrDefault ( x => x.UserName == userName );

		public IList<TeleBotUser> this [ UserRole role ] =>
			listImplementation.Where ( x => x.Role.HasFlag ( role ) ).ToList ( );

		public TeleBotUserList ( string fileName )
		{
			this.fileName      = fileName;
			listImplementation = new List<TeleBotUser> ( );
			Load ( );
		}

		public IEnumerator<TeleBotUser> GetEnumerator ( ) => listImplementation.GetEnumerator ( );

		IEnumerator IEnumerable.GetEnumerator ( ) => ( (IEnumerable) listImplementation ).GetEnumerator ( );

		public void Add ( TeleBotUser user )
		{
			lock ( listLock )
				listImplementation = listImplementation.Union ( new[] {user} ).ToList ( );

			Save ( );
		}

		public void Clear ( )
		{
			lock ( listLock )
				listImplementation.Clear ( );
		}

		public bool Contains ( TeleBotUser user ) => listImplementation.Contains ( user );

		public void CopyTo ( TeleBotUser[] array, int arrayIndex ) => listImplementation.CopyTo ( array, arrayIndex );

		public bool Remove ( TeleBotUser user )
		{
			lock ( listLock )
				return listImplementation.Remove ( user );
		}

		public int Count => listImplementation.Count;

		public bool IsReadOnly => listImplementation.IsReadOnly;

		public int IndexOf ( TeleBotUser user ) => listImplementation.IndexOf ( user );

		public void Insert ( int index, TeleBotUser item )
		{
			lock ( listLock )
				listImplementation.Insert ( index, item );
		}

		public void RemoveAt ( int index )
		{
			lock ( listLock )
				listImplementation.RemoveAt ( index );
		}

		public TeleBotUser this [ int index ]
		{
			get => listImplementation[index];
			set => listImplementation[index] = value;
		}

		public bool Contains ( string userName ) => listImplementation.Any ( x => x.UserName == userName );

		public bool Remove ( string userName )
		{
			if ( !Contains ( userName ) )
				return false;

			lock ( listLock )
				listImplementation = listImplementation.Where ( x => x.UserName != userName ).ToList ( );
			return true;
		}

		public bool HasFlag ( string userName, UserRole role ) =>
			this[userName]?.Role.HasFlag ( role ) == true;

		public void Load ( )
		{
			if ( !File.Exists ( fileName ) )
			{
				Logger.Warn ( new FileNotFoundException ( "Users file not found.", fileName ) );
				Save ( );
				return;
			}

			lock ( fileLock )
			{
				var json = File.ReadAllText ( fileName );
				lock ( listLock )
					listImplementation = JsonConvert.DeserializeObject<List<TeleBotUser>> ( json );
			}
		}

		public void Save ( )
		{
			var json = JsonConvert.SerializeObject ( listImplementation, Formatting.Indented );
			lock ( fileLock )
				File.WriteAllText ( fileName, json );
		}
	}
}