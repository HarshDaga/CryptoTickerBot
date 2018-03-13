using CryptoTickerBot.Exchanges.Core;

namespace CryptoTickerBot.WebSocket.Messages
{
	public class CryptoCoinSummary
	{
		public string Symbol { get; }
		public string ExchangeName { get; }
		public decimal HighestBid { get; }
		public decimal LowestAsk { get; }

		public CryptoCoinSummary ( CryptoExchangeBase exchange, CryptoCoin coin )
		{
			Symbol       = coin.Symbol;
			ExchangeName = exchange.Name;
			HighestBid   = coin.HighestBid;
			LowestAsk    = coin.LowestAsk;
		}
	}
}