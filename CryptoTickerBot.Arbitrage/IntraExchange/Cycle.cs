using System.Collections.Generic;
using CryptoTickerBot.Arbitrage.Abstractions;

namespace CryptoTickerBot.Arbitrage.IntraExchange
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
}