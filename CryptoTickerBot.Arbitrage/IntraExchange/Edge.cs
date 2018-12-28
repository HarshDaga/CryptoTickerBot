using CryptoTickerBot.Arbitrage.Abstractions;
using CryptoTickerBot.Arbitrage.Interfaces;

namespace CryptoTickerBot.Arbitrage.IntraExchange
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