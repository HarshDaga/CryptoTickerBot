using System.ComponentModel;

namespace CryptoTickerBot.Data.Enums
{
	public enum CryptoCoinId
	{
		[Description ( "Not a coin" )] NULL = 0,
		BTC = 1,
		ETH,
		BCH,
		LTC,
		XRP,
		NEO,
		DASH,
		XMR,
		TRX,
		ETC,
		OMG,
		ZEC,
		XLM,
		BNB,
		BTG,
		BCD,
		IOT,
		DOGE,
		STEEM
	}
}