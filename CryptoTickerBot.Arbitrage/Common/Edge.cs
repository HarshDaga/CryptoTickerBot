using CryptoTickerBot.Arbitrage.Abstractions;
using CryptoTickerBot.Arbitrage.Interfaces;

namespace CryptoTickerBot.Arbitrage.Common
{
	public class Edge : EdgeBase
	{
		public Edge ( INode from,
		              INode to,
		              decimal cost ) : base ( from, to, cost )
		{
		}
	}
}