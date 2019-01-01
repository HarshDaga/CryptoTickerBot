using System;
using System.Linq;
using CryptoTickerBot.Arbitrage.Common;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace CryptoTickerBot.UnitTests.ArbitrageTests
{
	[TestFixture]
	public class NodeTests
	{
		[Test]
		public void NodeAddOrUpdateEdgeShouldNotOverwriteAnExistingEdgeReference ( )
		{
			var nodeA = new Node ( "A" );
			var nodeB = new Node ( "B" );
			var edgeAb = new Edge ( nodeA, nodeB, 42m );
			var edgeAbDup = new Edge ( nodeA, nodeB, 0m );

			Assert.True ( nodeA.AddOrUpdateEdge ( edgeAb ) );
			Assert.True ( nodeA.HasEdge ( "B" ) );
			Assert.AreSame ( nodeA["B"], edgeAb );
			Assert.AreEqual ( nodeA["B"].OriginalCost, 42m );
			Assert.False ( nodeA.AddOrUpdateEdge ( edgeAbDup ) );
			Assert.AreSame ( nodeA["B"], edgeAb );
			Assert.AreEqual ( nodeA["B"].OriginalCost, edgeAbDup.OriginalCost );
			Assert.AreEqual ( nodeA.Edges.Count ( ), 1 );
		}

		[Test]
		public void NodeAddOrUpdateEdgeShouldRejectEdgesFromOtherNodes ( )
		{
			var nodeA = new Node ( "A" );
			var nodeB = new Node ( "B" );
			var nodeC = new Node ( "C" );
			var nodeD = new Node ( "D" );
			var edgeAb = new Edge ( nodeA, nodeB, 42m );
			var edgeCd = new Edge ( nodeC, nodeD, 0m );

			Assert.True ( nodeA.AddOrUpdateEdge ( edgeAb ) );
			Assert.False ( nodeB.AddOrUpdateEdge ( edgeAb ) );
			Assert.False ( nodeA.AddOrUpdateEdge ( edgeCd ) );
			Assert.False ( nodeC.AddOrUpdateEdge ( edgeAb ) );
			Assert.True ( nodeC.AddOrUpdateEdge ( edgeCd ) );
			Assert.AreEqual ( nodeA.Edges.Count ( ), 1 );
			Assert.True ( nodeA.EdgeTable.Keys.Single ( ) == "B" );
		}

		[Test]
		public void NodesShouldBeOrderedBySymbol ( )
		{
			var randomizer = Randomizer.CreateRandomizer ( );
			var symbols = Enumerable.Range ( 0, 10000 ).Select ( x => randomizer.GetString ( 20 ) ).ToList ( );
			var nodes = symbols.Select ( x => new Node ( x ) ).ToList ( );

			var result = symbols.OrderBy ( x => x, StringComparer.OrdinalIgnoreCase )
				.SequenceEqual ( nodes.OrderBy ( x => x ).Select ( x => x.Symbol ) );

			Assert.True ( result );
		}

		[Test]
		public void NodesWithSameSymbolShouldBeEqual ( )
		{
			var nodeA = new Node ( "A" );
			var nodeB = new Node ( "B" );
			var nodeDupA = new Node ( "A" );

			Assert.AreEqual ( nodeA, nodeA );
			Assert.AreNotEqual ( nodeA, nodeB );
			Assert.AreEqual ( nodeA, nodeDupA );
			Assert.AreNotEqual ( nodeA, nodeB );
			Assert.AreNotEqual ( nodeB, nodeDupA );
			Assert.AreEqual ( nodeA.GetHashCode ( ), nodeDupA.GetHashCode ( ) );
			Assert.AreEqual ( nodeA, nodeDupA );
			Assert.AreNotSame ( nodeA, nodeDupA );
		}
	}
}