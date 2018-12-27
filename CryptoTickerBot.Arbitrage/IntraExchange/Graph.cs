using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Arbitrage.Abstractions;
using CryptoTickerBot.Arbitrage.Interfaces;
using CryptoTickerBot.Data.Domain;
using MoreLinq;

namespace CryptoTickerBot.Arbitrage.IntraExchange
{
	public class Graph : GraphBase<Node>
	{
		public CryptoExchangeId ExchangeId { get; }
		public ISet<string> BaseSymbols { get; }
		public double Delta { get; }

		public override EdgeBuilderDelegate<Node, IEdge> DefaultEdgeBuilder { get; protected set; } =
			( from,
			  to,
			  cost ) =>
				new Edge ( from, to, cost );

		public Graph ( CryptoExchangeId exchangeId,
		               IEnumerable<string> baseSymbols,
		               double delta,
		               NodeBuilderDelegate<Node> nodeBuilder ) : base ( nodeBuilder )
		{
			ExchangeId  = exchangeId;
			BaseSymbols = new HashSet<string> ( baseSymbols );
			Delta       = delta;
		}

		public List<ICycle<Node>> GetTriangularCycles ( )
		{
			var cycles = new List<ICycle<Node>> ( );

			var pairs = BaseSymbols
				.Select ( x => Nodes[x] )
				.Subsets ( 2 )
				.SelectMany ( x => new[] {( x[0], x[1] ), ( x[1], x[0] )} )
				.Where ( x => x.Item2.HasEdge ( x.Item1.Symbol ) )
				.ToList ( );

			foreach ( var pair in pairs )
				cycles.AddRange ( GetTriangularCycles ( pair ) );

			return cycles.Distinct ( ).ToList ( );
		}

		public static IEnumerable<ICycle<Node>> GetTriangularCycles ( (Node, Node) pair )
		{
			var (from, to) = pair;

			var nodes = from.Edges
				.Where ( x => x.To.EdgeTable.ContainsKey ( to.Symbol ) )
				.Select ( x => x.To )
				.OfType<Node> ( );

			foreach ( var node in nodes )
				yield return new Cycle ( from, node, to, from );
		}
	}
}