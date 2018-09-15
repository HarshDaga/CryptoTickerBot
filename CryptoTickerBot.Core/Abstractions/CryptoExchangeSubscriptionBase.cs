using System;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Data.Domain;
using Humanizer;
using Humanizer.Localisation;

namespace CryptoTickerBot.Core.Abstractions
{
	public abstract class CryptoExchangeSubscriptionBase :
		ICryptoExchangeSubscription,
		IEquatable<CryptoExchangeSubscriptionBase>
	{
		public Guid Id { get; } = Guid.NewGuid ( );

		public ICryptoExchange Exchange { get; protected set; }

		public DateTime CreationTime { get; }

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
			$"{nameof ( Id )}: {Id}," +
			$" {nameof ( Exchange )}: {Exchange}," +
			$" {nameof ( ActiveSince )}: {ActiveSince.Humanize ( 4, minUnit: TimeUnit.Second )}";

		protected virtual void Start ( ICryptoExchange exchange )
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
			return Id.Equals ( other.Id );
		}

		public bool Equals ( CryptoExchangeSubscriptionBase other )
		{
			if ( other is null ) return false;
			if ( ReferenceEquals ( this, other ) ) return true;
			return Id.Equals ( other.Id );
		}

		public override bool Equals ( object obj )
		{
			if ( obj is null ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;
			return obj is CryptoExchangeSubscriptionBase other && Equals ( other );
		}

		public override int GetHashCode ( ) => Id.GetHashCode ( );

		public static bool operator == ( CryptoExchangeSubscriptionBase left,
		                                 CryptoExchangeSubscriptionBase right ) => Equals ( left, right );

		public static bool operator != ( CryptoExchangeSubscriptionBase left,
		                                 CryptoExchangeSubscriptionBase right ) => !Equals ( left, right );

		#endregion
	}
}