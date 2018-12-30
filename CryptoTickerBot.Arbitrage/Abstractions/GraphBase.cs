using System.Collections.Immutable;
using CryptoTickerBot.Arbitrage.Interfaces;

namespace CryptoTickerBot.Arbitrage.Abstractions
{
	public abstract class GraphBase<TNode> : IGraph<TNode> where TNode : class, INode
	{
		public ImmutableDictionary<string, TNode> Nodes { get; protected set; } =
			ImmutableDictionary<string, TNode>.Empty;

		public abstract EdgeBuilderDelegate<TNode, IEdge> DefaultEdgeBuilder { get; protected set; }

		public TNode this [ string symbol ] =>
			Nodes.TryGetValue ( symbol, out var value ) ? value : null;

		public NodeBuilderDelegate<TNode> NodeBuilder { get; }

		protected GraphBase ( NodeBuilderDelegate<TNode> nodeBuilder )
		{
			NodeBuilder = nodeBuilder;
		}

		public bool ContainsNode ( string symbol ) =>
			Nodes.ContainsKey ( symbol );

		public virtual TNode AddNode ( string symbol )
		{
			if ( Nodes.TryGetValue ( symbol, out var node ) )
				return node;

			node  = NodeBuilder ( symbol );
			Nodes = Nodes.SetItem ( symbol, node );

			return node;
		}

		public virtual IEdge UpsertEdge ( string from,
		                                  string to,
		                                  decimal cost ) =>
			UpsertEdge ( from, to, cost, DefaultEdgeBuilder );

		public virtual TEdge UpsertEdge<TEdge> ( string from,
		                                         string to,
		                                         decimal cost,
		                                         EdgeBuilderDelegate<TNode, TEdge> edgeBuilder )
			where TEdge : class, IEdge
		{
			var nodeFrom = AddNode ( from );
			var nodeTo = AddNode ( to );

			var edge = edgeBuilder ( nodeFrom, nodeTo, cost );
			if ( nodeFrom.AddOrUpdateEdge ( edge ) )
				OnEdgeInsert ( nodeFrom, nodeTo );
			else
				OnEdgeUpdate ( nodeFrom, nodeTo );

			return nodeFrom[to] as TEdge;
		}

		protected abstract void OnEdgeInsert ( TNode from,
		                                       TNode to );

		protected abstract void OnEdgeUpdate ( TNode from,
		                                       TNode to );
	}
}