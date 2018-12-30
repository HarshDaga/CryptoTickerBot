using System;
using System.Collections.Immutable;

namespace CryptoTickerBot.Arbitrage.Interfaces
{
	public interface ICycle<TNode> : IEquatable<ICycle<TNode>> where TNode : INode
	{
		ImmutableList<TNode> Path { get; }
		ImmutableList<IEdge> Edges { get; }

		int Length { get; }
		double Weight { get; }

		double UpdateWeight ( );
	}
}