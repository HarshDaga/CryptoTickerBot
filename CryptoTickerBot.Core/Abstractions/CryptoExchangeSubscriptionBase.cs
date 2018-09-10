using System;
using CryptoTickerBot.Core.Interfaces;
using Humanizer;
using Humanizer.Localisation;
using Newtonsoft.Json;

namespace CryptoTickerBot.Core.Abstractions
{
	public abstract class CryptoExchangeSubscriptionBase :
		ICryptoExchangeSubscription,
		IEquatable<CryptoExchangeSubscriptionBase>
	{
		public Guid Guid { get; } = Guid.NewGuid ( );

		[JsonIgnore]
		public ICryptoExchange Exchange { get; protected set; }

		public DateTime CreationTime { get; }

		[JsonIgnore]
		public TimeSpan ActiveSince => DateTime.UtcNow - CreationTime;

		protected CryptoExchangeSubscriptionBase ( )
		{
			CreationTime = DateTime.UtcNow;
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

		public override string ToString ( ) =>
			$"{nameof ( Guid )}: {Guid}," +
			$" {nameof ( Exchange )}: {Exchange}," +
			$" {nameof ( ActiveSince )}: {ActiveSince.Humanize ( 4, minUnit: TimeUnit.Second )}";

		protected void Start ( ICryptoExchange exchange )
		{
			if ( exchange is null )
				return;

			Exchange = exchange;
			Exchange.Subscribe ( this );
		}

		#region Equality Members

		public bool Equals ( ICryptoExchangeSubscription other )
		{
			if ( other is null ) return false;
			if ( ReferenceEquals ( this, other ) ) return true;
			return Guid.Equals ( other.Guid );
		}

		public bool Equals ( CryptoExchangeSubscriptionBase other )
		{
			if ( other is null ) return false;
			if ( ReferenceEquals ( this, other ) ) return true;
			return Guid.Equals ( other.Guid );
		}

		public override bool Equals ( object obj )
		{
			if ( obj is null ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;
			return obj is CryptoExchangeSubscriptionBase other && Equals ( other );
		}

		public override int GetHashCode ( ) => Guid.GetHashCode ( );

		public static bool operator == ( CryptoExchangeSubscriptionBase left,
		                                 CryptoExchangeSubscriptionBase right ) => Equals ( left, right );

		public static bool operator != ( CryptoExchangeSubscriptionBase left,
		                                 CryptoExchangeSubscriptionBase right ) => !Equals ( left, right );

		#endregion
	}
}