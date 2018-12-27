namespace CryptoTickerBot.Arbitrage.Interfaces
{
	public interface IEdge
	{
		INode From { get; }
		INode To { get; }
		double OriginalCost { get; }

		double Weight { get; }
	}
}