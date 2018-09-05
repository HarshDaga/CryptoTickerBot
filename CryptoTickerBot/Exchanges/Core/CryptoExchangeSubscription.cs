using System;

namespace CryptoTickerBot.Exchanges.Core
{
	public abstract class CryptoExchangeSubscription : IDisposable, IObserver<CryptoCoin>
	{
		public CryptoExchangeBase Exchange { get; }
		public DateTime CreationTime { get; }
		public TimeSpan ActiveSince => DateTime.UtcNow - CreationTime;
		public IDisposable Disposable;

		protected CryptoExchangeSubscription ( CryptoExchangeBase exchange )
		{
			Exchange     = exchange;
			CreationTime = DateTime.UtcNow;
		}

		public void Dispose ( ) => Disposable?.Dispose ( );

		public abstract void OnNext ( CryptoCoin value );

		public virtual void OnError ( Exception error )
		{
		}

		public virtual void OnCompleted ( )
		{
		}
	}
}