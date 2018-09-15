using System;
using CryptoTickerBot.Data.Domain;
using Newtonsoft.Json;

namespace CryptoTickerBot.Core.Interfaces
{
	public interface ICryptoExchangeSubscription :
		IDisposable,
		IObserver<CryptoCoin>,
		IEquatable<ICryptoExchangeSubscription>
	{
		Guid Id { get; }

		[JsonIgnore]
		ICryptoExchange Exchange { get; }

		DateTime CreationTime { get; }

		[JsonIgnore]
		TimeSpan ActiveSince { get; }

		void Stop ( );
	}
}