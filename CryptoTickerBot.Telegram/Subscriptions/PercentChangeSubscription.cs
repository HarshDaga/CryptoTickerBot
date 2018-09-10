using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTickerBot.Core;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Domain;
using CryptoTickerBot.Telegram.Extensions;
using Humanizer;
using Humanizer.Localisation;
using Newtonsoft.Json;
using NLog;
using Telegram.Bot.Types;

namespace CryptoTickerBot.Telegram.Subscriptions
{
	public class PercentChangeSubscription : CryptoExchangeSubscriptionBase
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public Guid Guid { get; } = Guid.NewGuid ( );
		public Chat Chat { get; }
		public User User { get; }
		public CryptoExchangeId ExchangeId { get; }
		public decimal Threshold { get; set; }
		public IDictionary<string, CryptoCoin> LastSignificantPrice { get; private set; }
		public ImmutableHashSet<string> Symbols { get; private set; }

		[JsonIgnore]
		public TelegramBot TelegramBot { get; private set; }

		public PercentChangeSubscription ( Chat chat,
		                                   User user,
		                                   CryptoExchangeId exchangeId,
		                                   decimal threshold,
		                                   IDictionary<string, CryptoCoin> lastSignificantPrice,
		                                   IEnumerable<string> symbols )
		{
			Chat       = chat;
			User       = user;
			ExchangeId = exchangeId;
			Threshold  = threshold;
			Symbols    = ImmutableHashSet<string>.Empty.Union ( symbols );

			LastSignificantPrice = new ConcurrentDictionary<string, CryptoCoin> (
				lastSignificantPrice
					.Where ( x => Symbols.Contains ( x.Key ) )
			);
		}

		public ImmutableHashSet<string> AddCoins ( IEnumerable<string> symbols ) =>
			Symbols = Symbols.Union ( symbols.Select ( x => x.ToUpper ( ) ) );

		public ImmutableHashSet<string> RemoveCoins ( IEnumerable<string> symbols ) =>
			Symbols = Symbols.Except ( symbols.Select ( x => x.ToUpper ( ) ) );

		public void Start ( TelegramBot telegramBot )
		{
			TelegramBot = telegramBot;

			if ( !TelegramBot.Ctb.TryGetExchange ( ExchangeId, out var exchange ) )
				return;

			if ( LastSignificantPrice is null )
				LastSignificantPrice = new ConcurrentDictionary<string, CryptoCoin> (
					exchange.ExchangeData
						.Where ( x => Symbols.Contains ( x.Key ) )
				);

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
				.SendTextBlockAsync ( Chat, builder.ToString ( ) )
				.ConfigureAwait ( false );
		}
	}
}