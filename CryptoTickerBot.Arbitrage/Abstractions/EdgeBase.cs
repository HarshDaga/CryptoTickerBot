using System;
using CryptoTickerBot.Arbitrage.Interfaces;

namespace CryptoTickerBot.Arbitrage.Abstractions
{
	public abstract class EdgeBase : IEdge
	{
		public virtual INode From { get; }
		public virtual INode To { get; }
		public decimal OriginalCost { get; protected set; }

		public virtual double Weight => -Math.Log ( (double) OriginalCost );

		protected EdgeBase ( INode from,
		                     INode to,
		                     decimal cost )
		{
			From         = from;
			To           = to;
			OriginalCost = cost;
		}

		public virtual void CopyFrom ( IEdge edge )
		{
			OriginalCost = edge.OriginalCost;
		}

		public override string ToString ( ) => $"{From.Symbol} -> {To.Symbol}  {OriginalCost} {Weight}";
	}
}