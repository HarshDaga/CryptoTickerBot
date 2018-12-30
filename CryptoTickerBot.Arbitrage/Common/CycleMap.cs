using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using CryptoTickerBot.Arbitrage.Interfaces;

namespace CryptoTickerBot.Arbitrage.Common
{
	using static ImmutableHashSet;

	public class CycleMap<TNode> where TNode : INode
	{
		public ImmutableHashSet<ICycle<TNode>> this [ string from,
		                                              string to ]
		{
			get
			{
				if ( !data.TryGetValue ( from, out var dict ) )
					return null;
				return dict.TryGetValue ( to, out var set ) ? set : null;
			}
		}

		public ImmutableHashSet<ICycle<TNode>> this [ INode from,
		                                              INode to ] =>
			this[from.Symbol, to.Symbol];

		private readonly ConcurrentDictionary<string,
			ConcurrentDictionary<string,
				ImmutableHashSet<ICycle<TNode>>>> data
			=
			new ConcurrentDictionary<string,
				ConcurrentDictionary<string,
					ImmutableHashSet<ICycle<TNode>>>> ( );

		public bool AddCycle ( string from,
		                       string to,
		                       ICycle<TNode> cycle )
		{
			if ( !data.TryGetValue ( from, out var dict ) )
			{
				data[from] = new ConcurrentDictionary<string, ImmutableHashSet<ICycle<TNode>>>
					{[to] = ImmutableHashSet<ICycle<TNode>>.Empty.Add ( cycle )};
				return true;
			}

			if ( dict.TryGetValue ( to, out var storedCycles ) )
			{
				var builder = storedCycles.ToBuilder ( );
				var result = builder.Add ( cycle );
				dict[to] = builder.ToImmutable ( );
				return result;
			}

			dict[to] = ImmutableHashSet<ICycle<TNode>>.Empty.Add ( cycle );
			return true;
		}

		public void AddCycles ( string from,
		                        string to,
		                        IEnumerable<ICycle<TNode>> cycles )
		{
			if ( !data.TryGetValue ( from, out var dict ) )
			{
				data[from] = new ConcurrentDictionary<string, ImmutableHashSet<ICycle<TNode>>>
					{[to] = cycles.ToImmutableHashSet ( )};
				return;
			}

			if ( dict.TryGetValue ( to, out var storedCycles ) )
			{
				var builder = storedCycles.ToBuilder ( );
				builder.UnionWith ( cycles );
				dict[to] = builder.ToImmutable ( );
				return;
			}

			dict[to] = cycles.ToImmutableHashSet ( );
		}
	}
}