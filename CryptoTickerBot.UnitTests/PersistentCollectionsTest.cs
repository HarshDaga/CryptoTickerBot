using System;
using System.IO;
using CryptoTickerBot.Collections.Persistent;
using NUnit.Framework;

namespace CryptoTickerBot.UnitTests
{
	[TestFixture]
	public class PersistentCollectionsTest
	{
		[SetUp]
		public void Setup ( )
		{
			foreach ( var fileName in new[] {ListFileName, SetFileName, DictionaryFileName} )
				if ( File.Exists ( fileName ) )
					File.Delete ( fileName );
		}

		private const string ListFileName = "PersistentList.json";
		private const string SetFileName = "PersistentSet.json";
		private const string DictionaryFileName = "PersistentDictionary.json";

		private static PersistentList<T> MakeList<T> ( ) =>
			PersistentList<T>.Build ( ListFileName );

		private static PersistentSet<T> MakeSet<T> ( ) =>
			PersistentSet<T>.Build ( SetFileName );

		private static PersistentDictionary<TKey, TValue> MakeDictionary<TKey, TValue> ( ) =>
			PersistentDictionary<TKey, TValue>.Build ( DictionaryFileName );

		[Test]
		public void PersistentCollectionsShouldBeUniquelyIdentifiedByFileName ( )
		{
			using ( var first = MakeList<int> ( ) )
			using ( var second = MakeList<int> ( ) )
			{
				Assert.AreSame ( first, second );
				Assert.Throws<InvalidCastException> ( ( ) => MakeList<string> ( ) );
				Assert.Throws<InvalidCastException> ( ( ) => PersistentSet<char>.Build ( ListFileName ) );
				Assert.DoesNotThrow ( ( ) => MakeSet<string> ( )?.Dispose ( ) );
				Assert.DoesNotThrow ( ( ) => MakeDictionary<string, string> ( )?.Dispose ( ) );
			}
		}

		[Test]
		public void PersistentListShouldCreateFileWithCorrectName ( )
		{
			using ( var list = MakeList<int> ( ) )
			{
				list.ForceSave ( );
				Assert.True ( File.Exists ( list.FileName ) );
			}
		}

		[Test]
		public void PersistentListShouldPersistDataInMemory ( )
		{
			using ( var list = MakeList<int> ( ) )
			{
				Assert.IsEmpty ( list );
				list.AddWithoutSaving ( 1 );
				Assert.That ( list.Count, Is.EqualTo ( 1 ) );
				list.AddWithoutSaving ( 1 );
				Assert.That ( list.Count, Is.EqualTo ( 2 ) );
			}
		}

		[Test]
		public void PersistentListShouldPersistDataOnDisk ( )
		{
			using ( var list = MakeList<int> ( ) )
			{
				Assert.IsEmpty ( list );
				list.Add ( 1 );
				Assert.That ( list.Count, Is.EqualTo ( 1 ) );
			}

			using ( var list = MakeList<int> ( ) )
			{
				Assert.That ( list.Count, Is.EqualTo ( 1 ) );
				list.Add ( 2 );
				Assert.That ( list.Count, Is.EqualTo ( 2 ) );
				Assert.That ( list[1], Is.EqualTo ( 2 ) );
			}
		}

		[Test]
		public void PersistentSetShouldFollowSetLogic ( )
		{
			using ( var set = MakeSet<int> ( ) )
			{
				Assert.IsEmpty ( set );
				Assert.True ( set.AddOrUpdate ( 1 ) );
				Assert.That ( set.Count, Is.EqualTo ( 1 ) );
			}
		}
	}
}