using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTickerBot.Core;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Core.Extensions;
using CryptoTickerBot.Domain;
using CryptoTickerBot.Telegram.Extensions;
using Humanizer;
using Humanizer.Localisation;
using Newtonsoft.Json;
using NLog;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CryptoTickerBot.Telegram.Subscriptions
{
	public class PercentChangeSubscription :
		CryptoExchangeSubscriptionBase,
		IEquatable<PercentChangeSubscription>
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public ChatId ChatId { get; }
		public User User { get; }
		public CryptoExchangeId ExchangeId { get; }

		public decimal Threshold { get; set; }
		public IDictionary<string, CryptoCoin> LastSignificantPrice { get; private set; }
		public ImmutableHashSet<string> Symbols { get; private set; }

		[JsonIgnore]
		public TelegramBot TelegramBot { get; private set; }

		private Chat chat;

		public PercentChangeSubscription ( ChatId chatId,
		                                   User user,
		                                   CryptoExchangeId exchangeId,
		                                   decimal threshold,
		                                   IDictionary<string, CryptoCoin> lastSignificantPrice,
		                                   IEnumerable<string> symbols )
		{
			ChatId     = chatId;
			User       = user;
			ExchangeId = exchangeId;
			Threshold  = threshold;
			Symbols    = ImmutableHashSet<string>.Empty.Union ( symbols );

			LastSignificantPrice = new ConcurrentDictionary<string, CryptoCoin> (
				lastSignificantPrice
					.Where ( x => Symbols.Contains ( x.Key ) )
			);
		}

		public override string ToString ( ) =>
			$"{nameof ( User )}: {User}," +
			$" {( chat.Type == ChatType.Private ? "" : $"{chat.Title}," )}" +
			$" {nameof ( Exchange )}: {ExchangeId}," +
			$" {nameof ( Threshold )}: {Threshold:P}," +
			$" {nameof ( Symbols )}: {Symbols.Join ( ", " )}";

		public ImmutableHashSet<string> AddCoins ( IEnumerable<string> symbols ) =>
			Symbols = Symbols.Union ( symbols.Select ( x => x.ToUpper ( ) ) );

		public ImmutableHashSet<string> RemoveCoins ( IEnumerable<string> symbols ) =>
			Symbols = Symbols.Except ( symbols.Select ( x => x.ToUpper ( ) ) );

		public async Task Start ( TelegramBot telegramBot )
		{
			TelegramBot = telegramBot;

			if ( !TelegramBot.Ctb.TryGetExchange ( ExchangeId, out var exchange ) )
				return;

			if ( LastSignificantPrice is null )
				LastSignificantPrice = new ConcurrentDictionary<string, CryptoCoin> (
					exchange.ExchangeData
						.Where ( x => Symbols.Contains ( x.Key ) )
				);

			chat = await TelegramBot.Client.GetChatAsync ( ChatId, TelegramBot.Ctb.Cts.Token );

			Start ( exchange );
		}

		public override async void OnNext ( CryptoCoin coin )
		{
			if ( !Symbols.Contains ( coin.Symbol ) )
				return;

			if ( !LastSignificantPrice.ContainsKey ( coin.Symbol ) )
				LastSignificantPrice[coin.Symbol] = coin;

			var change = coin - LastSignificantPrice[coin.Symbol];
			var percentage = Math.Abs ( change.Percentage );

			if ( percentage >= Threshold )
			{
				await SendMessage ( LastSignificantPrice[coin.Symbol].Clone ( ), coin.Clone ( ) )
					.ConfigureAwait ( false );

				LastSignificantPrice[coin.Symbol] = coin.Clone ( );
			}
		}

		private async Task SendMessage ( CryptoCoin previous,
		                                 CryptoCoin next )
		{
			Logger.Debug (
				$"Invoked subscription for {User} @ {next.Rate:C} {next.Symbol} {Exchange.Name}"
			);

			var change = next - previous;
			var builder = new StringBuilder ( );
			builder
				.AppendLine ( $"{Exchange.Name,-14} {next.Symbol}" )
				.AppendLine ( $"Current Price: {next.Rate:C}" )
				.AppendLine ( $"Change:        {change.Value}" )
				.AppendLine ( $"Change %:      {change.Percentage:P}" )
				.AppendLine ( $"in {change.TimeDiff.Humanize ( 3, minUnit: TimeUnit.Second )}" );

			await TelegramBot.Client
				.SendTextBlockAsync ( ChatId, builder.ToString ( ) )
				.ConfigureAwait ( false );
		}

		#region Equality Members

		public bool Equals ( PercentChangeSubscription other )
		{
			if ( other is null ) return false;
			return ReferenceEquals ( this, other ) || Guid.Equals ( other.Guid );
		}

		public override bool Equals ( object obj )
		{
			if ( obj is null ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;
			return obj.GetType ( ) == GetType ( ) && Equals ( (PercentChangeSubscription) obj );
		}

		public override int GetHashCode ( ) => Guid.GetHashCode ( );

		public static bool operator == ( PercentChangeSubscription left,
		                                 PercentChangeSubscription right ) => Equals ( left, right );

		public static bool operator != ( PercentChangeSubscription left,
		                                 PercentChangeSubscription right ) => !Equals ( left, right );

		#endregion
	}
}