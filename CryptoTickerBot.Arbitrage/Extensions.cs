using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Arbitrage.Common;
using CryptoTickerBot.Arbitrage.Interfaces;
using MoreLinq;

namespace CryptoTickerBot.Arbitrage
{
	public static class Extensions
	{
		public static bool IsCyclicEquivalent<T> ( this IReadOnlyList<T> cycle1,
		                                           IReadOnlyList<T> cycle2 )
		{
			return cycle1
				.Skip ( 1 )
				.Concat ( cycle1.Skip ( 1 ) )
				.Window ( cycle2.Count - 1 )
				.Any ( x => x.SequenceEqual ( cycle2.Skip ( 1 ) ) );
		}

		public static bool IsCyclicEquivalent<TNode> ( this ICycle<TNode> cycle1,
		                                               ICycle<TNode> cycle2 ) where TNode : INode =>
			IsCyclicEquivalent ( cycle1.Path, cycle2.Path );

		public static IEnumerable<ICycle<TNode>> GetTriangularCycles<TNode> ( this TNode from,
		                                                                      TNode to )
			where TNode : INode =>
			GetTriangularCycles ( ( from, to ) );

		public static IEnumerable<ICycle<TNode>> GetTriangularCycles<TNode> ( this (TNode, TNode) pair )
			where TNode : INode
		{
			var (to, from) = pair;

			if ( !to.HasEdge ( from.Symbol ) )
				yield break;

			var nodes = from.Edges
				.Where ( x => x.To.EdgeTable.ContainsKey ( to.Symbol ) )
				.Select ( x => x.To )
				.OfType<TNode> ( );

			foreach ( var node in nodes )
				yield return new Cycle<TNode> ( from, node, to, from );
		}


		public static List<ICycle<TNode>> GetAllTriangularCycles<TNode> ( this IGraph<TNode> graph )
			where TNode : INode
		{
			var cycles = new List<ICycle<TNode>> ( );

			var pairs = graph.Nodes.Values
				.Subsets ( 2 )
				.SelectMany ( x => new[] {( x[0], x[1] ), ( x[1], x[0] )} )
				.Where ( x => x.Item2.HasEdge ( x.Item1.Symbol ) )
				.ToList ( );

			foreach ( var pair in pairs )
				cycles.AddRange ( GetTriangularCycles ( pair ) );

			return cycles.Distinct ( ).ToList ( );
		}
	}
}