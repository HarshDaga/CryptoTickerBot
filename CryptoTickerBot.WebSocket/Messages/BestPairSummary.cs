using CryptoTickerBot.Helpers;

namespace CryptoTickerBot.WebSocket.Messages
{
	public class BestPairSummary
	{
		public string From { get; }
		public string To { get; }
		public string FirstCoin { get; }
		public string SecondCoin { get; }
		public decimal Profit { get; }
		public decimal Fees { get; }

		public BestPairSummary ( CryptoCompareTable table )
		{
			var (from, to, first, second, profit, fees) = table.GetBest ( );

			From       = from.ToString ( );
			To         = to.ToString ( );
			FirstCoin  = first.ToString ( );
			SecondCoin = second.ToString ( );
			Profit     = profit;
			Fees       = fees;
		}
	}
}