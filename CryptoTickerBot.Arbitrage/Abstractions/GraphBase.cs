using System.Collections.Concurrent;
using System.Collections.Generic;
using CryptoTickerBot.Arbitrage.Interfaces;

namespace CryptoTickerBot.Arbitrage.Abstractions
{
	public abstract class GraphBase<TNode> : IGraph<TNode> where TNode : class, INode
	{
		protected virtual ConcurrentDictionary<string, TNode> Nodes { get; set; } =
			new ConcurrentDictionary<string, TNode> ( );

		IDictionary<string, TNode> IGraph<TNode>.Nodes => Nodes;
		public virtual EdgeBuilderDelegate<TNode, IEdge> DefaultEdgeBuilder { get; protected set; }

		public TNode this [ string symbol ]
		{
			get => Nodes.TryGetValue ( symbol, out var value ) ? value : null;
			set => Nodes[symbol] = value;
		}

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

			node          = NodeBuilder ( symbol );
			Nodes[symbol] = node;

			return node;
		}

		public virtual IEdge UpsertEdge ( string from,
		                                  string to,
		                                  double cost ) =>
			UpsertEdge ( from, to, cost, DefaultEdgeBuilder );

		public virtual TEdge UpsertEdge<TEdge> ( string from,
		                                         string to,
		                                         double cost,
		                                         EdgeBuilderDelegate<TNode, TEdge> edgeBuilder ) where TEdge : IEdge
		{
			var nodeFrom = AddNode ( from );
			var nodeTo = AddNode ( to );

			var edge = edgeBuilder ( nodeFrom, nodeTo, cost );
			nodeFrom.AddEdge ( edge );

			return edge;
		}
	}
}