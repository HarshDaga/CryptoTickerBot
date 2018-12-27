using System;
using System.Collections.Generic;

namespace CryptoTickerBot.Arbitrage.Interfaces
{
	public interface INode : IEquatable<INode>, IComparable<INode>
	{
		string Symbol { get; }
		IReadOnlyDictionary<string, IEdge> EdgeTable { get; }
		IEnumerable<IEdge> Edges { get; }

		IEdge this [ string symbol ] { get; }

		void AddEdge ( IEdge edge );
		bool HasEdge ( string symbol );
	}
}