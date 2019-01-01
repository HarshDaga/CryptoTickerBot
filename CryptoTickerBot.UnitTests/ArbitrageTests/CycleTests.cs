using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Arbitrage;
using CryptoTickerBot.Arbitrage.Common;
using CryptoTickerBot.Arbitrage.Interfaces;
using NUnit.Framework;

namespace CryptoTickerBot.UnitTests.ArbitrageTests
{
	[TestFixture]
	public class CycleTests
	{
		public static Dictionary<string, Node> MakeNodes ( IEnumerable<string> symbols ) =>
			symbols.ToDictionary ( x => x, x => new Node ( x ) );

		public static Dictionary<string, Node> MakeNodes ( params string[] symbols ) =>
			MakeNodes ( symbols.AsEnumerable ( ) );

		public static IEdge ConnectNodes ( Node node1,
		                                   Node node2,
		                                   decimal cost )
		{
			var edge = new Edge ( node1, node2, cost );
			node1.AddOrUpdateEdge ( edge );

			return edge;
		}

		public static void ConnectNodes ( Dictionary<string, Node> nodeMap,
		                                  params (string from, string to, decimal cost)[] pairs )
		{
			foreach ( var (from, to, cost) in pairs )
				ConnectNodes ( nodeMap[from], nodeMap[to], cost );
		}

		public static Cycle<Node> GetCycle ( Dictionary<string, Node> nodeMap,
		                                     params string[] symbols )
		{
			var nodes = new List<Node> ( symbols.Length );
			foreach ( var symbol in symbols )
				if ( nodeMap.TryGetValue ( symbol, out var node ) )
					nodes.Add ( node );

			return new Cycle<Node> ( nodes );
		}

		[Test]
		public void CycleConstructorShouldThrowOnBrokenPath ( )
		{
			var nodeMap = MakeNodes ( "A", "B", "C", "D" );
			ConnectNodes ( nodeMap,
			               ( "A", "B", 1m ),
			               ( "B", "C", 1m ),
			               ( "C", "D", 1m ),
			               ( "D", "A", 1m )
			);

			Assert.Throws<ArgumentOutOfRangeException> ( ( ) => GetCycle ( nodeMap, "A", "B", "C", "A" ) );
			Assert.DoesNotThrow ( ( ) => GetCycle ( nodeMap, "A", "B", "C", "D", "A" ) );
		}

		[Test]
		public void CycleShouldHaveCorrectEdgeList ( )
		{
			var nodeMap = MakeNodes ( "A", "B", "C", "D" );
			ConnectNodes ( nodeMap,
			               ( "A", "B", 1m ),
			               ( "B", "C", 1m ),
			               ( "C", "D", 1m ),
			               ( "D", "A", 1m )
			);

			var cycle = GetCycle ( nodeMap, "A", "B", "C", "D", "A" );
			Assert.AreEqual ( cycle.Length, 4 );
			Assert.True ( cycle.Path.Select ( x => x.Symbol ).SequenceEqual ( new[] {"A", "B", "C", "D", "A"} ) );
			Assert.AreEqual ( cycle.Edges.Count, 4 );

			INode cur = nodeMap["A"];
			foreach ( var edge in cycle.Edges )
			{
				Assert.AreSame ( cur, edge.From );
				cur = edge.To;
			}

			Assert.AreSame ( cur, nodeMap["A"] );
		}

		[Test]
		public void CycleUpdateWeightShouldRecomputeWeightCorrectly ( )
		{
			var nodeMap = MakeNodes ( "A", "B", "C", "D" );
			ConnectNodes ( nodeMap,
			               ( "A", "B", 1m ),
			               ( "B", "C", 1m ),
			               ( "C", "D", 1m ),
			               ( "D", "A", 1m )
			);

			var cycle = GetCycle ( nodeMap, "A", "B", "C", "D", "A" );
			var weight = cycle.UpdateWeight ( );
			Assert.That ( weight, Is.EqualTo ( 0 ).Within ( 0.000001 ) );
			Assert.AreEqual ( weight, cycle.UpdateWeight ( ) );
			Assert.AreEqual ( weight, cycle.Weight );

			ConnectNodes ( nodeMap, ( "D", "A", 1.5m ) );
			Assert.That ( cycle.UpdateWeight ( ), Is.LessThan ( 0 ) );

			ConnectNodes ( nodeMap, ( "D", "A", 0.5m ) );
			Assert.That ( cycle.UpdateWeight ( ), Is.GreaterThan ( 0 ) );
		}

		[Test]
		public void RotatedCyclesShouldBeEqual ( )
		{
			var nodeMap = MakeNodes ( "A", "B", "C", "D" );
			ConnectNodes ( nodeMap,
			               ( "A", "B", 1m ),
			               ( "B", "C", 1m ),
			               ( "C", "D", 1m ),
			               ( "D", "A", 1m )
			);

			var cycleA = GetCycle ( nodeMap, "A", "B", "C", "D", "A" );
			var cycleB = GetCycle ( nodeMap, "B", "C", "D", "A", "B" );

			Assert.AreEqual ( cycleA, cycleA );
			Assert.AreNotSame ( cycleA, cycleB );
			Assert.AreEqual ( cycleA, cycleB );
			Assert.AreEqual ( cycleA.GetHashCode ( ), cycleB.GetHashCode ( ) );
			Assert.True ( cycleA.IsCyclicEquivalent ( cycleB ) );
		}
	}
}