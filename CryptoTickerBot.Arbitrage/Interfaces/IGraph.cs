using System.Collections.Generic;

namespace CryptoTickerBot.Arbitrage.Interfaces
{
	public interface IGraph<TNode> where TNode : INode
	{
		IDictionary<string, TNode> Nodes { get; }

		TNode this [ string symbol ] { get; }
		NodeBuilderDelegate<TNode> NodeBuilder { get; }
		EdgeBuilderDelegate<TNode, IEdge> DefaultEdgeBuilder { get; }

		bool ContainsNode ( string symbol );
		TNode AddNode ( string symbol );

		IEdge UpsertEdge ( string from,
		                   string to,
		                   double cost );

		TEdge UpsertEdge<TEdge> ( string from,
		                          string to,
		                          double cost,
		                          EdgeBuilderDelegate<TNode, TEdge> edgeBuilder ) where TEdge : IEdge;
	}

	public delegate TNode NodeBuilderDelegate<out TNode> ( string symbol );

	public delegate TEdge EdgeBuilderDelegate<in TNode, out TEdge> ( TNode from,
	                                                                 TNode to,
	                                                                 double cost );
}