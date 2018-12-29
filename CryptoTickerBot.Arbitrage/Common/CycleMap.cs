using System.Collections.Concurrent;
using System.Collections.Generic;
using CryptoTickerBot.Arbitrage.Interfaces;

namespace CryptoTickerBot.Arbitrage.Common
{
	public class CycleMap<TNode> where TNode : INode
	{
		public HashSet<ICycle<TNode>> this [ TNode from,
		                                     TNode to ]
		{
			get
			{
				if ( !data.TryGetValue ( from, out var dict ) )
					return null;
				return dict.TryGetValue ( to, out var set ) ? set : null;
			}
			set
			{
				if ( !data.TryGetValue ( from, out var dict ) )
				{
					data[from] = new ConcurrentDictionary<TNode, HashSet<ICycle<TNode>>> {[to] = value};
					return;
				}

				if ( dict.TryGetValue ( to, out var cycles ) )
					cycles.UnionWith ( value );
				else
					dict[to] = value;
			}
		}

		private readonly ConcurrentDictionary<TNode, ConcurrentDictionary<TNode, HashSet<ICycle<TNode>>>> data =
			new ConcurrentDictionary<TNode, ConcurrentDictionary<TNode, HashSet<ICycle<TNode>>>> ( );

		public bool AddCycle ( TNode from,
		                       TNode to,
		                       ICycle<TNode> value )
		{
			if ( !data.TryGetValue ( from, out var dict ) )
			{
				data[from] = new ConcurrentDictionary<TNode, HashSet<ICycle<TNode>>>
					{[to] = new HashSet<ICycle<TNode>> {value}};
				return true;
			}

			if ( dict.TryGetValue ( to, out var cycles ) )
				return cycles.Add ( value );

			dict[to] = new HashSet<ICycle<TNode>> {value};
			return true;
		}
	}
}