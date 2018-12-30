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

		public IList<ICycle<Node>> AllCycles
		{
			get
			{
				lock ( cycleLock )
					return allCycles.ToList ( );
			}
		}

		public CycleMap<Node> CycleMap { get; } = new CycleMap<Node> ( );

		public override EdgeBuilderDelegate<Node, IEdge> DefaultEdgeBuilder { get; protected set; } =
			( from,
			  to,
			  cost ) =>
				new Edge ( from, to, cost );

		private readonly object cycleLock = new object ( );
		private readonly HashSet<ICycle<Node>> allCycles = new HashSet<ICycle<Node>> ( );

		public Graph ( CryptoExchangeId exchangeId ) :
			base ( symbol => new Node ( symbol ) )
		{
			ExchangeId = exchangeId;
		}

		public event NegativeCycleFoundDelegate NegativeCycleFound;

		private void UpdateCycleMap ( IEnumerable<ICycle<Node>> cycles )
		{
			foreach ( var cycle in cycles )
			foreach ( var pair in cycle.Path.Window ( 2 ) )
				CycleMap.AddCycle ( pair[0].Symbol, pair[1].Symbol, cycle );
		}

		protected override void OnEdgeInsert ( Node from,
		                                       Node to )
		{
			lock ( cycleLock )
			{
				var cycles = ( from, to ).GetTriangularCycles ( ).ToList ( );
				allCycles.UnionWith ( cycles );
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