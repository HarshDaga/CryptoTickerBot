using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Arbitrage.Interfaces;
using MoreLinq;

namespace CryptoTickerBot.Arbitrage
{
	internal static class Utility
	{
		public static bool IsCyclicEquivalent<TNode> ( ICycle<TNode> cycle1,
		                                               ICycle<TNode> cycle2 ) where TNode : INode =>
			IsCyclicEquivalent ( cycle1.Path, cycle2.Path );

		public static bool IsCyclicEquivalent<T> ( IReadOnlyList<T> cycle1,
		                                           IReadOnlyList<T> cycle2 )
		{
			return cycle1
				.Skip ( 1 )
				.Concat ( cycle1.Skip ( 1 ) )
				.Window ( cycle2.Count - 1 )
				.Any ( x => x.SequenceEqual ( cycle2.Skip ( 1 ) ) );
		}
	}
}