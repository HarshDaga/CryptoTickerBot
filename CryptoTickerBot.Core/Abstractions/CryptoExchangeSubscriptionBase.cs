using System;
using CryptoTickerBot.Core.Interfaces;
using Newtonsoft.Json;

namespace CryptoTickerBot.Core.Abstractions
{
	public abstract class CryptoExchangeSubscriptionBase : ICryptoExchangeSubscription
	{
		[JsonIgnore]
		public ICryptoExchange Exchange { get; protected set; }

		public DateTime CreationTime { get; }

		[JsonIgnore]
		public TimeSpan ActiveSince => DateTime.UtcNow - CreationTime;

		protected CryptoExchangeSubscriptionBase ( )
		{
			CreationTime = DateTime.UtcNow;
		}

		protected void Start ( ICryptoExchange exchange )
		{
			if ( exchange is null )
				return;

			Exchange = exchange;
			Exchange.Subscribe ( this );
		}

		public virtual void Stop ( )
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