using System;

namespace CryptoTickerBot.Domain
{
	public interface ICryptoCoin
	{
		decimal HighestBid { get; set; }
		decimal LowestAsk { get; set; }
		decimal Rate { get; set; }
		string Symbol { get; }
		DateTime Time { get; set; }
	}
}