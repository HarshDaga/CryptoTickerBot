using System;
using System.Collections.Generic;

namespace CryptoTickerBot.Arbitrage.Interfaces
{
	public interface ICycle<TNode> : IEquatable<ICycle<TNode>> where TNode : INode
	{
		IReadOnlyList<TNode> Path { get; }

		int Length { get; }
		double Weight { get; }

		double UpdateWeight ( );

		bool Contains ( TNode node1,
		                TNode node2 );
	}
}