using System;
using System.Collections.Generic;
using CryptoTickerBot.Arbitrage.Interfaces;

namespace CryptoTickerBot.Arbitrage.Abstractions
{
	public abstract class NodeBase : INode
	{
		public string Symbol { get; }

		protected virtual IDictionary<string, IEdge> EdgeTableImpl { get; } =
			new Dictionary<string, IEdge> ( );

		public IReadOnlyDictionary<string, IEdge> EdgeTable =>
			EdgeTableImpl as IReadOnlyDictionary<string, IEdge>;

		public IEnumerable<IEdge> Edges => EdgeTableImpl.Values;

		public IEdge this [ string symbol ] =>
			EdgeTableImpl.TryGetValue ( symbol, out var value ) ? value : null;

		protected NodeBase ( string symbol )
		{
			Symbol = symbol;
		}

		public bool AddOrUpdateEdge ( IEdge edge )
		{
			if ( !Equals ( edge.From ) )
				return false;

			if ( EdgeTableImpl.TryGetValue ( edge.To.Symbol, out var existing ) )
			{
				existing.CopyFrom ( edge );
				return false;
			}

			EdgeTableImpl[edge.To.Symbol] = edge;
			return true;
		}

		public bool HasEdge ( string symbol ) =>
			EdgeTableImpl.ContainsKey ( symbol );

		#region Auto Generated

		public bool Equals ( INode other )
		{
			if ( other is null ) return false;
			if ( ReferenceEquals ( this, other ) ) return true;
			return string.Equals ( Symbol, other.Symbol );
		}

		public int CompareTo ( INode other )
		{
			if ( ReferenceEquals ( this, other ) ) return 0;
			if ( other is null ) return 1;
			return string.Compare ( Symbol, other.Symbol, StringComparison.OrdinalIgnoreCase );
		}

		public int CompareTo ( NodeBase other ) =>
			CompareTo ( other as INode );

		public int CompareTo ( object obj )
		{
			if ( obj is null ) return 1;
			if ( ReferenceEquals ( this, obj ) ) return 0;
			return obj is INode other
				? CompareTo ( other )
				: throw new ArgumentException ( $"Object must be of type {nameof ( NodeBase )}" );
		}

		public static bool operator < ( NodeBase left,
		                                NodeBase right ) =>
			Comparer<NodeBase>.Default.Compare ( left, right ) < 0;

		public static bool operator > ( NodeBase left,
		                                NodeBase right ) =>
			Comparer<NodeBase>.Default.Compare ( left, right ) > 0;

		public static bool operator <= ( NodeBase left,
		                                 NodeBase right ) =>
			Comparer<NodeBase>.Default.Compare ( left, right ) <= 0;

		public static bool operator >= ( NodeBase left,
		                                 NodeBase right ) =>
			Comparer<NodeBase>.Default.Compare ( left, right ) >= 0;

		public override bool Equals ( object obj )
		{
			if ( obj is null ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;
			if ( obj.GetType ( ) != GetType ( ) ) return false;
			return Equals ( (NodeBase) obj );
		}

		public override int GetHashCode ( ) =>
			Symbol != null ? Symbol.GetHashCode ( ) : 0;

		public override string ToString ( ) =>
			$"{Symbol,-6} {EdgeTableImpl.Count}";

		#endregion
	}
}