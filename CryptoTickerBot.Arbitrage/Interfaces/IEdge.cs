namespace CryptoTickerBot.Arbitrage.Interfaces
{
	public interface IEdge
	{
		INode From { get; }
		INode To { get; }
		decimal OriginalCost { get; }

		double Weight { get; }
		void CopyFrom ( IEdge edge );
	}
}