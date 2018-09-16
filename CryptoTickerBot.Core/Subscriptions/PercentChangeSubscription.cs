using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Extensions;
using CryptoTickerBot.Data.Helpers;
using Fody;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;

namespace CryptoTickerBot.Core.Subscriptions
{
	[ConfigureAwait ( false )]
	public class PercentChangeSubscription :
		CryptoExchangeSubscriptionBase,
		IEquatable<PercentChangeSubscription>
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		[JsonConverter ( typeof ( StringEnumConverter ) )]
		public CryptoExchangeId ExchangeId { get; }

		public decimal Threshold { get; set; }
		public IDictionary<string, CryptoCoin> LastSignificantPrice { get; protected set; }
		public ImmutableHashSet<string> Symbols { get; protected set; }

		public PercentChangeSubscription ( CryptoExchangeId exchangeId,
		                                   decimal threshold,
		                                   IEnumerable<string> symbols )
		{
			ExchangeId = exchangeId;
			Threshold  = threshold;
			Symbols    = ImmutableHashSet<string>.Empty.Union ( symbols.Select ( x => x.ToUpper ( ) ) );
		}

		public event TriggerDelegate Trigger;

		public override string ToString ( ) =>
			$" {nameof ( Exchange )}: {ExchangeId}," +
			$" {nameof ( Threshold )}: {Threshold:P}," +
			$" {nameof ( Symbols )}: {Symbols.Join ( ", " )}";

		public ImmutableHashSet<string> AddSymbols ( IEnumerable<string> symbols ) =>
			Symbols = Symbols.Union ( symbols.Select ( x => x.ToUpper ( ) ) );

		public ImmutableHashSet<string> RemoveSymbols ( IEnumerable<string> symbols ) =>
			Symbols = Symbols.Except ( symbols.Select ( x => x.ToUpper ( ) ) );

		protected override void Start ( ICryptoExchange exchange )
		{
			if ( exchange is null )
				return;

			base.Start ( exchange );

			if ( LastSignificantPrice is null )
				LastSignificantPrice = new ConcurrentDictionary<string, CryptoCoin> (
					exchange.ExchangeData
						.Where ( x => Symbols.Contains ( x.Key ) )
				);
		}

		public override async void OnNext ( CryptoCoin coin )
		{
			if ( !Symbols.Contains ( coin.Symbol ) )
				return;

			if ( !LastSignificantPrice.ContainsKey ( coin.Symbol ) )
				LastSignificantPrice[coin.Symbol] = coin;

			var change = PriceChange.Difference ( coin, LastSignificantPrice[coin.Symbol] );
			var percentage = Math.Abs ( change.Percentage );

			if ( percentage >= Threshold )
			{
				var previous = LastSignificantPrice[coin.Symbol].Clone ( );
				LastSignificantPrice[coin.Symbol] = coin.Clone ( );

				await OnTrigger ( previous.Clone ( ), coin.Clone ( ) );
				Trigger?.Invoke ( this, previous.Clone ( ), coin.Clone ( ) );
			}
		}

		protected virtual Task OnTrigger ( CryptoCoin old,
		                                   CryptoCoin current ) =>
			Task.CompletedTask;

		#region Equality Members

		public bool Equals ( PercentChangeSubscription other )
		{
			if ( other is null ) return false;
			return ReferenceEquals ( this, other ) || Id.Equals ( other.Id );
		}

		public override bool Equals ( object obj )
		{
			if ( obj is null ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;
			return obj.GetType ( ) == GetType ( ) && Equals ( (PercentChangeSubscription) obj );
		}

		public override int GetHashCode ( ) => Id.GetHashCode ( );

		public static bool operator == ( PercentChangeSubscription left,
		                                 PercentChangeSubscription right ) => Equals ( left, right );

		public static bool operator != ( PercentChangeSubscription left,
		                                 PercentChangeSubscription right ) => !Equals ( left, right );

		#endregion
	}

	public delegate Task TriggerDelegate ( PercentChangeSubscription subscription,
	                                       CryptoCoin old,
	                                       CryptoCoin current );
}