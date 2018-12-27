using System;
using CryptoTickerBot.Arbitrage.Interfaces;

namespace CryptoTickerBot.Arbitrage.Abstractions
{
	public abstract class EdgeBase : IEdge
	{
		public virtual INode From { get; }
		public virtual INode To { get; }
		public double OriginalCost { get; }

		public virtual double Weight => -Math.Log ( OriginalCost );

		protected EdgeBase ( INode from,
		                     INode to,
		                     double cost )
		{
			From         = from;
			To           = to;
			OriginalCost = cost;
		}

		public override string ToString ( ) => $"{From.Symbol} -> {To.Symbol}  {OriginalCost} {Weight}";
	}
}