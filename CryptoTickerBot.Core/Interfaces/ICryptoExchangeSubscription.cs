using System;

namespace CryptoTickerBot.Core.Interfaces
{
	public interface ICryptoExchangeSubscription :
		IDisposable, IObserver<CryptoCoin>
	{
		ICryptoExchange Exchange { get; }
		DateTime CreationTime { get; }
		TimeSpan ActiveSince { get; }

		void Stop ( );
	}
}