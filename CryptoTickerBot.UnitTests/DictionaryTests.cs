using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CryptoTickerBot.UnitTests
{
	[TestFixture]
	public class DictionaryTests
	{
		[Test]
		public void DictionaryRetainsInsertionOrder ( )
		{
			var random = new Random ( 42 );
			var normal = new Dictionary<string, int> ( );
			var keys = new List<string> ( 10000 );
			foreach ( var _ in Enumerable.Range ( 1, 10000 ) )
			{
				var value = random.Next ( 100, int.MaxValue );
				var key = $"{value}";
				keys.Add ( key );
				normal[key] = value;
			}

			var json = JsonConvert.SerializeObject ( normal );
			var rebuilt = JsonConvert.DeserializeObject<Dictionary<string, string>> ( json );
			Assert.True ( new Dictionary<string, int> ( normal ).Keys.SequenceEqual ( rebuilt.Keys ) );
			Assert.True ( keys.SequenceEqual ( rebuilt.Keys ) );
		}
	}
}