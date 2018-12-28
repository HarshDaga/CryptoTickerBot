using System;
using System.Collections.Generic;
using CryptoTickerBot.Arbitrage.Interfaces;

namespace CryptoTickerBot.Arbitrage.Abstractions
{
	public abstract class NodeBase : INode
	{
		public string Symbol { get; }

		protected Dictionary<string, IEdge> EdgeTable { get; set; } =
			new Dictionary<string, IEdge> ( );

		IReadOnlyDictionary<string, IEdge> INode.EdgeTable => EdgeTable;
		public IEnumerable<IEdge> Edges => EdgeTable.Values;

		public IEdge this [ string symbol ]
		{
			get => EdgeTable.TryGetValue ( symbol, out var value ) ? value : null;
			protected set => EdgeTable[symbol] = value;
		}

		protected NodeBase ( string symbol )
		{
			Symbol = symbol;
		}

		public bool AddOrUpdateEdge ( IEdge edge )
		{
			if ( EdgeTable.TryGetValue ( edge.To.Symbol, out var existing ) )
			{
				existing.CopyFrom ( edge );
				return false;
			}

			EdgeTable[edge.To.Symbol] = edge;
			return true;
		}

		public bool HasEdge ( string symbol ) =>
			EdgeTable.ContainsKey ( symbol );

		#region Auto Generated

		public bool Equals ( INode other )
		{
			if ( other is null ) return false;
			if ( ReferenceEquals ( this, other ) ) return true;
			return string.Equals ( Symbol, other.Symbol );
		}

		public int CompareTo ( INode other ) => 0;

		public int CompareTo ( NodeBase other )
		{
			if ( ReferenceEquals ( this, other ) ) return 0;
			if ( other is null ) return 1;
			return string.Compare ( Symbol, other.Symbol, StringComparison.OrdinalIgnoreCase );
		}

		public int CompareTo ( object obj )
		{
			if ( obj is null ) return 1;
			if ( ReferenceEquals ( this, obj ) ) return 0;
			return obj is NodeBase other
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

		public override int GetHashCode ( ) => Symbol != null ? Symbol.GetHashCode ( ) : 0;

		public static bool operator == ( NodeBase left,
		                                 NodeBase right ) =>
			Equals ( left, right );

		public static bool operator != ( NodeBase left,
		                                 NodeBase right ) =>
			!Equals ( left, right );

		public override string ToString ( ) => $"{Symbol,-6} {EdgeTable.Count}";

		#endregion
	}
}