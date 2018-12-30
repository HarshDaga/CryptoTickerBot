using CryptoTickerBot.Arbitrage.Common;
using NUnit.Framework;

namespace CryptoTickerBot.UnitTests.ArbitrageTests
{
	[TestFixture]
	public class EdgeTests
	{
		[Test]
		public void EdgeCopyFromShouldNotChangeNodeReferences ( )
		{
			var nodeA = new Node ( "A" );
			var nodeB = new Node ( "B" );
			var nodeC = new Node ( "C" );
			var nodeD = new Node ( "D" );
			var edgeAb = new Edge ( nodeA, nodeB, 42m );
			var edgeCd = new Edge ( nodeC, nodeD, 0m );

			Assert.AreSame ( edgeCd.From, nodeC );
			Assert.AreSame ( edgeCd.To, nodeD );
			Assert.AreNotEqual ( edgeAb.To, edgeCd.To );
			Assert.AreEqual ( edgeAb.OriginalCost, 42m );
			Assert.AreEqual ( edgeCd.OriginalCost, 0m );

			edgeCd.CopyFrom ( edgeAb );
			Assert.AreSame ( edgeCd.From, nodeC );
			Assert.AreSame ( edgeCd.To, nodeD );
			Assert.AreEqual ( edgeAb.OriginalCost, 42m );
			Assert.AreEqual ( edgeCd.OriginalCost, 42m );
		}
	}
}