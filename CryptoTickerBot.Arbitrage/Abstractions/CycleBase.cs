using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CryptoTickerBot.Arbitrage.Interfaces;
using MoreLinq;

namespace CryptoTickerBot.Arbitrage.Abstractions
{
	public abstract class CycleBase<TNode> : ICycle<TNode> where TNode : INode
	{
		public ImmutableList<TNode> Path { get; }
		public ImmutableList<IEdge> Edges { get; }
		public int Length => Path.Count - 1;
		public double Weight { get; protected set; } = double.PositiveInfinity;

		protected CycleBase ( IEnumerable<TNode> path )
		{
			Path  = ImmutableList<TNode>.Empty.AddRange ( path );
			Edges = ImmutableList<IEdge>.Empty.AddRange ( Path.Window ( 2 ).Select ( x => x[0][x[1].Symbol] ) );

			if ( Edges.Any ( x => x is null ) )
				throw new ArgumentOutOfRangeException ( );
		}

		public virtual double UpdateWeight ( ) =>
			Weight = Edges.Sum ( x => x.Weight );

		public override string ToString ( ) =>
			string.Join ( " -> ", Path.Select ( x => x.Symbol ) );

		#region Equality Comparers

		bool IEquatable<ICycle<TNode>>.Equals ( ICycle<TNode> other ) =>
			other != null && other.Path.IsCyclicEquivalent ( Path );

		public override bool Equals ( object obj )
		{
			if ( obj is null ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;

			return obj is ICycle<TNode> cycle && Equals ( cycle );
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

		#endregion
	}
}