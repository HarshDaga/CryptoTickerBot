using System.Collections.Generic;
using CryptoTickerBot.Arbitrage.Abstractions;
using CryptoTickerBot.Arbitrage.Interfaces;

namespace CryptoTickerBot.Arbitrage.Common
{
	public class Cycle : CycleBase<Node>
	{
		public Cycle ( IEnumerable<Node> path ) : base ( path )
		{
		}

		public Cycle ( params Node[] path ) : base ( path )
		{
		}
	}

	public class Cycle<TNode> : CycleBase<TNode> where TNode : INode
	{
		public Cycle ( IEnumerable<TNode> path ) : base ( path )
		{
		}

		public Cycle ( params TNode[] path ) : base ( path )
		{
		}
	}
}