using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Arbitrage.Abstractions;
using CryptoTickerBot.Arbitrage.Common;
using CryptoTickerBot.Arbitrage.Interfaces;
using CryptoTickerBot.Data.Domain;
using MoreLinq;

namespace CryptoTickerBot.Arbitrage.IntraExchange
{
	public class Graph : GraphBase<Node>
	{
		public CryptoExchangeId ExchangeId { get; }

		public ISet<ICycle<Node>> Cycles { get; } = new HashSet<ICycle<Node>> ( );

		public CycleMap<Node> CycleMap { get; } = new CycleMap<Node> ( );

		public override EdgeBuilderDelegate<Node, IEdge> DefaultEdgeBuilder { get; protected set; } =
			( from,
			  to,
			  cost ) =>
				new Edge ( from, to, cost );

		private readonly object cycleLock = new object ( );

		public Graph ( CryptoExchangeId exchangeId ) :
			base ( symbol => new Node ( symbol ) )
		{
			ExchangeId = exchangeId;
		}

		public event NegativeCycleFoundDelegate NegativeCycleFound;

		public List<ICycle<Node>> GetAllTriangularCycles ( )
		{
			var cycles = new List<ICycle<Node>> ( );

			var pairs = Nodes.Values
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
			var (to, from) = pair;

			if ( !to.HasEdge ( from.Symbol ) )
				yield break;

			var nodes = from.Edges
				.Where ( x => x.To.EdgeTable.ContainsKey ( to.Symbol ) )
				.Select ( x => x.To )
				.OfType<Node> ( );

			foreach ( var node in nodes )
				yield return new Cycle ( from, node, to, from );
		}

		private void UpdateCycleMap ( IEnumerable<ICycle<Node>> cycles )
		{
			foreach ( var cycle in cycles )
			foreach ( var pair in cycle.Path.Window ( 2 ) )
				CycleMap.AddCycle ( pair[0], pair[1], cycle );
		}

		protected override void OnEdgeInsert ( Node from,
		                                       Node to )
		{
			lock ( cycleLock )
			{
				var cycles = GetTriangularCycles ( ( from, to ) ).ToList ( );
				Cycles.UnionWith ( cycles );
				UpdateCycleMap ( cycles );

				foreach ( var cycle in cycles )
					if ( cycle.UpdateWeight ( ) < 0 )
						NegativeCycleFound?.Invoke ( this, cycle );
			}
		}

		protected override void OnEdgeUpdate ( Node from,
		                                       Node to )
		{
			lock ( cycleLock )
			{
				var cycles = CycleMap[from, to];
				if ( cycles is null )
					return;
				foreach ( var cycle in cycles )
					if ( cycle.UpdateWeight ( ) < 0 )
						NegativeCycleFound?.Invoke ( this, cycle );
			}
		}

		public override string ToString ( ) => $"{ExchangeId}";
	}

	public delegate Task NegativeCycleFoundDelegate ( Graph graph,
	                                                  ICycle<Node> cycle );
}