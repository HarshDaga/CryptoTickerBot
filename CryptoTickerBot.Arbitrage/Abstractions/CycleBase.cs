using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Arbitrage.Interfaces;
using MoreLinq;

namespace CryptoTickerBot.Arbitrage.Abstractions
{
	public abstract class CycleBase<TNode> : ICycle<TNode> where TNode : INode
	{
		public List<TNode> Path { get; }
		public IEnumerable<IEdge> Edges => Path.Window ( 2 ).Select ( x => x[0][x[1].Symbol] );
		IReadOnlyList<TNode> ICycle<TNode>.Path => Path;
		public int Length => Path.Count - 1;
		public double Weight { get; protected set; } = double.PositiveInfinity;

		protected CycleBase ( IEnumerable<TNode> path )
		{
			Path = path.ToList ( );
		}

		public virtual double UpdateWeight ( )
		{
			Weight = 0;
			var node = Path[0];
			foreach ( var next in Path.Skip ( 1 ) )
			{
				Weight += node[next.Symbol].Weight;
				node   =  next;
			}

			return Weight;
		}

		public bool Contains ( TNode node1,
		                       TNode node2 )
		{
			var i = Path.IndexOf ( node1 );
			if ( i == -1 || Path.Count == i - 1 )
				return false;

			return Path[i + 1].Equals ( node2 );
		}

		public override string ToString ( ) =>
			string.Join ( " -> ", Path.Select ( x => x.Symbol ) );

		#region Equality Comparers

		bool IEquatable<ICycle<TNode>>.Equals ( ICycle<TNode> other ) =>
			other != null && Utility.IsCyclicEquivalent ( other.Path, Path );

		public override bool Equals ( object obj )
		{
			if ( obj is null ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;

			return obj is ICycle<TNode> cycle && Utility.IsCyclicEquivalent ( cycle.Path, Path );
		}

		public override int GetHashCode ( )
		{
			if ( Path is null )
				return 0;

			return Path
				.Skip ( 1 )
				.Aggregate ( 0, ( current,
				                  node ) => current ^ node.GetHashCode ( ) );
		}

		public static bool operator == ( CycleBase<TNode> left,
		                                 CycleBase<TNode> right ) =>
			Equals ( left, right );

		public static bool operator != ( CycleBase<TNode> left,
		                                 CycleBase<TNode> right ) =>
			!Equals ( left, right );

		#endregion
	}
}