using System;
using CryptoTickerBot.Core.Interfaces;

namespace CryptoTickerBot.Core.Abstractions
{
	public abstract class CryptoExchangeSubscriptionBase : ICryptoExchangeSubscription
	{
		public ICryptoExchange Exchange { get; }
		public DateTime CreationTime { get; }
		public TimeSpan ActiveSince => DateTime.UtcNow - CreationTime;

		protected CryptoExchangeSubscriptionBase ( ICryptoExchange exchange )
		{
			Exchange     = exchange;
			CreationTime = DateTime.UtcNow;
		}

		public void Start ( )
		{
			Exchange.Subscribe ( this );
		}

		public void Stop ( )
		{
			Exchange.Unsubscribe ( this );
		}

		public void Dispose ( ) => Stop ( );

		public virtual void OnError ( Exception error )
		{
		}

		public abstract void OnNext ( CryptoCoin coin );

		public virtual void OnCompleted ( )
		{
		}
	}
}