using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Subscriptions;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Extensions;
using CryptoTickerBot.Data.Helpers;
using CryptoTickerBot.Telegram.Extensions;
using Humanizer;
using Humanizer.Localisation;
using Newtonsoft.Json;
using NLog;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CryptoTickerBot.Telegram.Subscriptions
{
	public class TelegramPercentChangeSubscription :
		PercentChangeSubscription,
		IEquatable<TelegramPercentChangeSubscription>
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public ChatId ChatId { get; }
		public User User { get; }

		public bool IsSilent { get; set; }

		[JsonIgnore]
		public TelegramBot TelegramBot { get; private set; }

		private CancellationToken CancellationToken => TelegramBot.CancellationToken;

		private Chat chat;

		public TelegramPercentChangeSubscription ( ChatId chatId,
		                                           User user,
		                                           CryptoExchangeId exchangeId,
		                                           decimal threshold,
		                                           bool isSilent,
		                                           IEnumerable<string> symbols ) :
			base ( exchangeId, threshold, symbols )
		{
			ChatId    = chatId;
			User      = user;
			Threshold = threshold;
			IsSilent  = isSilent;
		}

		public override string ToString ( ) =>
			$"{nameof ( User )}: {User}," +
			$"{( chat.Type == ChatType.Private ? "" : $" {chat.Title}," )}" +
			$" {nameof ( Exchange )}: {ExchangeId}," +
			$" {nameof ( Threshold )}: {Threshold:P}," +
			$" {nameof ( IsSilent )}: {IsSilent}," +
			$" {nameof ( Symbols )}: {Symbols.Join ( ", " )}";

		public string Summary ( ) =>
			$"{nameof ( User )}: {User}\n" +
			$"{nameof ( Exchange )}: {ExchangeId}\n" +
			$"{nameof ( Threshold )}: {Threshold:P}\n" +
			$"Silent: {IsSilent}\n" +
			$"{nameof ( Symbols )}: {Symbols.Humanize ( )}";

		public async Task Start ( TelegramBot telegramBot,
		                          bool isBeingCreated = false )
		{
			TelegramBot = telegramBot;
			chat        = await TelegramBot.Client.GetChatAsync ( ChatId, CancellationToken );

			if ( !TelegramBot.Ctb.TryGetExchange ( ExchangeId, out var exchange ) )
				return;

			Start ( exchange );

			if ( isBeingCreated )
				await TelegramBot.Client.SendTextBlockAsync ( ChatId,
				                                              $"Created subscription:\n{Summary ( )}",
				                                              disableNotification: IsSilent,
				                                              cancellationToken: CancellationToken );
		}

		public async Task Resume ( TelegramBot telegramBot ) =>
			await Start ( telegramBot );

		public bool IsSimilarTo ( TelegramPercentChangeSubscription subscription ) =>
			User.Equals ( subscription.User ) &&
			ChatId.Identifier == subscription.ChatId.Identifier &&
			ExchangeId == subscription.ExchangeId;

		public async Task MergeWith ( TelegramPercentChangeSubscription subscription )
		{
			IsSilent = subscription.IsSilent;
			AddSymbols ( subscription.Symbols );

			await TelegramBot.Client
				.SendTextBlockAsync ( ChatId,
				                      $"Merged with subscription:\n{Summary ( )}",
				                      disableNotification: IsSilent,
				                      cancellationToken: CancellationToken )
				.ConfigureAwait ( false );
		}

		protected override async Task OnTrigger ( CryptoCoin old,
		                                          CryptoCoin current )
		{
			Logger.Debug (
				$"{Id} Invoked subscription for {User} @ {current.Rate:N} {current.Symbol} {Exchange.Name}"
			);

			var change = PriceChange.Difference ( current, old );
			var builder = new StringBuilder ( );
			builder
				.AppendLine ( $"{Exchange.Name,-14} {current.Symbol}" )
				.AppendLine ( $"Current Price: {current.Rate:N}" )
				.AppendLine ( $"Change:        {change.Value:N}" )
				.AppendLine ( $"Change %:      {change.Percentage:P}" )
				.AppendLine ( $"in {change.TimeDiff.Humanize ( 3, minUnit: TimeUnit.Second )}" );

			await TelegramBot.Client
				.SendTextBlockAsync ( ChatId,
				                      builder.ToString ( ),
				                      disableNotification: IsSilent,
				                      cancellationToken: CancellationToken )
				.ConfigureAwait ( false );
		}

		#region Equality Members

		public bool Equals ( TelegramPercentChangeSubscription other )
		{
			if ( other is null ) return false;
			return ReferenceEquals ( this, other ) || Id.Equals ( other.Id );
		}

		public override bool Equals ( object obj )
		{
			if ( obj is null ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;
			return obj.GetType ( ) == GetType ( ) && Equals ( (TelegramPercentChangeSubscription) obj );
		}

		public override int GetHashCode ( ) => Id.GetHashCode ( );

		public static bool operator == ( TelegramPercentChangeSubscription left,
		                                 TelegramPercentChangeSubscription right ) => Equals ( left, right );

		public static bool operator != ( TelegramPercentChangeSubscription left,
		                                 TelegramPercentChangeSubscription right ) => !Equals ( left, right );

		#endregion
	}
}