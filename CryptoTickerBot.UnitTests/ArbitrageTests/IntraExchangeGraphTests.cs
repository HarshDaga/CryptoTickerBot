using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Arbitrage;
using CryptoTickerBot.Arbitrage.Common;
using CryptoTickerBot.Arbitrage.Interfaces;
using CryptoTickerBot.Arbitrage.IntraExchange;
using CryptoTickerBot.Data.Domain;
using NUnit.Framework;

namespace CryptoTickerBot.UnitTests.ArbitrageTests
{
	[TestFixture]
	public class IntraExchangeGraphTests
	{
		private static bool IsCyclicEquivalent<TNode> ( ICycle<TNode> cycle,
		                                                params string[] symbols )
			where TNode : INode
		{
			return cycle
				.Path
				.Select ( x => x.Symbol )
				.ToList ( )
				.IsCyclicEquivalent ( symbols );
		}

		[Test]
		public void GraphShouldCreateNewNodeOnlyIfOneDoesNotExist ( )
		{
			var graph = new Graph ( CryptoExchangeId.Binance );
			Assert.AreEqual ( graph.Nodes.Count, 0 );

			var nodeA = graph.AddNode ( "A" );
			Assert.True ( graph.ContainsNode ( "A" ) );
			Assert.AreEqual ( nodeA.Symbol, "A" );
			Assert.AreEqual ( graph.Nodes.Count, 1 );

			var nodeDupA = graph.AddNode ( "A" );
			Assert.AreSame ( nodeA, nodeDupA );
			Assert.AreSame ( nodeA, graph["A"] );
			Assert.AreEqual ( graph.Nodes.Count, 1 );

			Assert.IsNull ( graph["B"] );
			var nodeB = graph.AddNode ( "B" );
			Assert.AreEqual ( nodeB.Symbol, "B" );
			Assert.AreEqual ( graph["B"], nodeB );
			Assert.AreEqual ( graph.Nodes.Count, 2 );
		}

		[Test]
		public void GraphShouldFindTriangularCyclesOnInsertions ( )
		{
			var graph = new Graph ( CryptoExchangeId.Binance );
			Assert.Zero ( graph.AllCycles.Count );

			graph.UpsertEdge ( "A", "B", 1m );
			Assert.IsNull ( graph.CycleMap["A", "B"] );
			graph.UpsertEdge ( "B", "C", 1m );
			graph.UpsertEdge ( "C", "A", 1m );

			Assert.AreEqual ( graph.AllCycles.Count, 1 );
			Assert.True ( IsCyclicEquivalent ( graph.AllCycles[0], "A", "B", "C", "A" ) );
			Assert.AreEqual ( graph.CycleMap["A", "B"].Count, 1 );
			Assert.AreEqual ( graph.CycleMap["A", "B"], graph.CycleMap["B", "C"] );
			Assert.AreEqual ( graph.CycleMap["C", "A"], graph.CycleMap["B", "C"] );
			Assert.IsNull ( graph.CycleMap["A", "C"] );

			graph.UpsertEdge ( "C", "D", 1m );
			graph.UpsertEdge ( "D", "A", 1m );
			graph.UpsertEdge ( "A", "C", 1m );
			Assert.AreEqual ( graph.AllCycles.Count, 2 );
			var cycles = graph.GetAllTriangularCycles ( );
			Assert.AreEqual ( cycles.Count, 2 );
			Assert.That ( cycles, Is.EquivalentTo ( graph.AllCycles ) );
		}

		[Test]
		public void GraphShouldTriggerEventOnCreatingNewNegativeCycle ( )
		{
			var graph = new Graph ( CryptoExchangeId.Binance );
			var count = 0;
			ICycle<Node> lastCycle = null;

			graph.NegativeCycleFound += ( g,
			                              cycle ) =>
			{
				++count;
				lastCycle = cycle;

				return Task.CompletedTask;
			};

			Assert.Zero ( count );
			Assert.IsNull ( lastCycle );

			graph.UpsertEdge ( "A", "B", 1m );
			graph.UpsertEdge ( "B", "C", 1m );
			Assert.Zero ( count );
			Assert.IsNull ( lastCycle );
			graph.UpsertEdge ( "C", "A", 1.5m );
			Assert.AreEqual ( count, 1 );
			Assert.IsNotNull ( lastCycle );
			Assert.True ( IsCyclicEquivalent ( lastCycle, "A", "B", "C", "A" ) );

			graph.UpsertEdge ( "C", "D", 1m );
			graph.UpsertEdge ( "D", "A", 1m );
			graph.UpsertEdge ( "A", "C", 1.5m );
			Assert.AreEqual ( count, 2 );
			Assert.IsNotNull ( lastCycle );
			Assert.True ( IsCyclicEquivalent ( lastCycle, "A", "C", "D", "A" ) );

			graph.UpsertEdge ( "B", "A", 1m );
			graph.UpsertEdge ( "C", "B", 0.5m );
			Assert.AreEqual ( count, 2 );
			Assert.True ( IsCyclicEquivalent ( lastCycle, "A", "C", "D", "A" ) );
		}

		[Test]
		public void GraphShouldTriggerEventOnExistingCycleBecomingNegative ( )
		{
			var graph = new Graph ( CryptoExchangeId.Binance );
			var count = 0;
			ICycle<Node> lastCycle = null;

			graph.NegativeCycleFound += ( g,
			                              cycle ) =>
			{
				++count;
				lastCycle = cycle;

				return Task.CompletedTask;
			};

			Assert.Zero ( count );
			Assert.IsNull ( lastCycle );

			graph.UpsertEdge ( "A", "B", 0m );
			graph.UpsertEdge ( "A", "B", 1m );
			graph.UpsertEdge ( "B", "C", 1m );
			graph.UpsertEdge ( "C", "A", 0.5m );
			Assert.Zero ( count );
			Assert.IsNull ( lastCycle );
			graph.UpsertEdge ( "C", "A", 1.5m );
			Assert.AreEqual ( count, 1 );
			Assert.IsNotNull ( lastCycle );
			Assert.True ( IsCyclicEquivalent ( lastCycle, "A", "B", "C", "A" ) );
		}

		[Test]
		public void GraphUpsertEdgeShouldCreateNodeIfNeeded ( )
		{
			var graph = new Graph ( CryptoExchangeId.Binance );
			Assert.AreEqual ( graph.Nodes.Count, 0 );

			var edge = graph.UpsertEdge ( "A", "B", 1m );
			Assert.AreEqual ( edge.From.Symbol, "A" );
			Assert.AreEqual ( edge.To.Symbol, "B" );
			Assert.AreEqual ( edge.OriginalCost, 1m );
			Assert.AreEqual ( graph.Nodes.Count, 2 );
			Assert.AreSame ( edge.From, graph["A"] );
			Assert.AreSame ( edge.To, graph["B"] );
		}

		[Test]
		public void GraphUpsertEdgeShouldReuseExistingNodes ( )
		{
			var graph = new Graph ( CryptoExchangeId.Binance );
			Assert.AreEqual ( graph.Nodes.Count, 0 );

			var edgeAb = graph.UpsertEdge ( "A", "B", 1m );
			var (nodeA, nodeB) = ( edgeAb.From, edgeAb.To );
			Assert.That ( graph.Nodes.Keys, Is.EquivalentTo ( new[] {"A", "B"} ) );
			Assert.That ( graph.Nodes.Values, Is.EquivalentTo ( new[] {nodeA, nodeB} ) );

			var edgeBc = graph.UpsertEdge ( "B", "C", 1m );
			var (nodeDupB, nodeC) = ( edgeBc.From, edgeBc.To );
			Assert.AreSame ( nodeB, nodeDupB );
			Assert.That ( graph.Nodes.Keys, Is.EquivalentTo ( new[] {"A", "B", "C"} ) );
			Assert.That ( graph.Nodes.Values, Is.EquivalentTo ( new[] {nodeA, nodeB, nodeC} ) );

			var edgeCa = graph.UpsertEdge ( "C", "A", 1m );
			var (nodeDupC, nodeDupA) = ( edgeCa.From, edgeCa.To );
			Assert.AreSame ( nodeC, nodeDupC );
			Assert.AreSame ( nodeA, nodeDupA );
			Assert.That ( graph.Nodes.Keys, Is.EquivalentTo ( new[] {"A", "B", "C"} ) );
			Assert.That ( graph.Nodes.Values, Is.EquivalentTo ( new[] {nodeA, nodeB, nodeC} ) );

			var edgeDupCa = graph.UpsertEdge ( "C", "A", 0m );
			Assert.AreSame ( edgeCa, edgeDupCa );
			Assert.AreEqual ( edgeCa.OriginalCost, 0m );
		}
	}
}