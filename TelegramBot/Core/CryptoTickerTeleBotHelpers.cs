using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Persistence;
using CryptoTickerBot.Exchanges.Core;
using CryptoTickerBot.Extensions;
using Tababular;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Extensions;
using CryptoCoin = CryptoTickerBot.CryptoCoin;

namespace TelegramBot.Core
{
	public partial class TeleBot
	{
		private readonly object subscriptionLock = new object ( );
		private readonly object alertLock = new object ( );
		public List<TelegramSubscription> Subscriptions { get; private set; }
		public List<TelegramPriceAlert> Alerts { get; private set; }

		private void ParseMessage ( Message message,
		                            out string command,
		                            out List<string> parameters )
		{
			var text = message.Text;
			command = text.Split ( ' ' ).First ( );
			if ( command.Contains ( $"@{me.Username}" ) )
				command = command.Substring ( 0, command.IndexOf ( $"@{me.Username}", StringComparison.Ordinal ) );
			parameters = text
				.Split ( new[] {' '}, StringSplitOptions.RemoveEmptyEntries )
				.Skip ( 1 )
				.ToList ( );
		}

		private async Task<bool> ValidateUserCommand ( User from,
		                                               string command,
		                                               Message message )
		{
			if ( !Users.Contains ( from.Id ) )
			{
				Logger.Info ( $"First message received from {from.Id}" );
				var user = new TelegramBotUser ( from );
				Users.Add ( user );
				UnitOfWork.Do ( u => u.Users.AddOrUpdate ( user ) );
			}

			if ( !commands.Keys.Contains ( command ) ) return true;

			if ( Settings.Instance.WhitelistMode && Users.Get ( from.Id )?.Role < UserRole.Registered )
			{
				await RequestPurchase ( message ).ConfigureAwait ( false );
				return true;
			}

			if ( Users.Get ( from.Id )?.Role < commands[command].role )
			{
				await SendBlockText ( message, $"You do not have access to {command}" ).ConfigureAwait ( false );
				return true;
			}

			return false;
		}

		[DebuggerStepThrough]
		private async Task SendBlockText ( Message message,
		                                   string str )
		{
			await bot.SendTextMessageAsync ( message.Chat.Id, $"```\n{str}\n```", ParseMode.Markdown )
				.ConfigureAwait ( false );
		}

		private async Task RequestPurchase ( Message message )
		{
			await SendBlockText ( message, "You need to purchase before you can use this command." )
				.ConfigureAwait ( false );
			await bot.SendTextMessageAsync (
				message.Chat.Id,
				$"{Settings.Instance.PurchaseMessageText}"
			).ConfigureAwait ( false );
		}

		private void SendSubscriptionReply (
			TelegramSubscription subscription,
			CryptoCoin oldValue,
			CryptoCoin newValue
		)
		{
			if ( oldValue is null )
				return;

			var change = newValue - oldValue;
			var builder = new StringBuilder ( );
			builder
				.AppendLine ( $"{subscription.Exchange.Name,-14} {newValue.Symbol}" )
				.AppendLine ( $"Current Price: {subscription.Exchange[newValue.Id].Average:C}" )
				.AppendLine ( $"Change:        {change.Value.ToCurrency ( )}" )
				.AppendLine ( $"Change %:      {change.Percentage:P}" )
				.AppendLine ( $"in {change.TimeDiff:dd\\:hh\\:mm\\:ss}" );

			bot.SendTextMessageAsync (
				subscription.ChatId,
				$"```\n{builder}\n```", ParseMode.Markdown
			);
		}

		private static Task UpdateSubscriptionInDb (
			TelegramSubscription subscription,
			CryptoCoin coin
		)
		{
			UnitOfWork.Do ( u =>
				{
					var ccv = coin.ToCryptoCoinValue ( subscription.ExchangeId );
					u.Subscriptions.UpdateCoin ( subscription.Id, ccv );
				}
			);

			return Task.CompletedTask;
		}

		private void SendPriceAlertReply (
			TelegramPriceAlert subscription,
			CryptoCoin oldValue,
			CryptoCoin newValue
		)
		{
			if ( oldValue is null )
				return;

			var change = newValue - oldValue;
			var builder = new StringBuilder ( );
			builder
				.AppendLine ( $"{subscription.Exchange.Name,-14} {newValue.Symbol}" )
				.AppendLine ( $"Current Price: {subscription.Exchange[newValue.Id].Average:C}" )
				.AppendLine ( $"Change:        {change.Value.ToCurrency ( )}" )
				.AppendLine ( $"Change %:      {change.Percentage:P}" )
				.AppendLine ( $"in {change.TimeDiff:dd\\:hh\\:mm\\:ss}" );

			bot.SendTextMessageAsync (
				subscription.ChatId,
				$"```\n{builder}\n```", ParseMode.Markdown
			);
		}

		private void AddAlert ( Message message,
		                        CryptoExchangeBase exchange,
		                        CryptoCoinId coinId,
		                        decimal price )
		{
			var alert = new TelegramPriceAlert ( exchange, message.Chat.Id,
			                                     message.From.Username, price, coinId );
			alert.Triggered += SendPriceAlertReply;
			alert.Triggered += ( priceAlert,
			                     prevPrice,
			                     newPrice ) =>
			{
				priceAlert.Dispose ( );
				Alerts.Remove ( priceAlert );
			};
			exchange.Subscribe ( alert );
			lock ( alertLock )
			{
				if ( Alerts is null )
					Alerts = new List<TelegramPriceAlert> ( );
				Alerts.Add ( alert );
			}
		}

		private void StartSubscription ( CryptoExchangeBase exchange,
		                                 TeleSubscription sub )
		{
			var subscription = new TelegramSubscription ( exchange, sub );
			subscription.Changed += SendSubscriptionReply;
			subscription.Changed += async ( s,
			                                o,
			                                n ) => await UpdateSubscriptionInDb ( s, n ).ConfigureAwait ( false );
			exchange.Subscribe ( subscription );
			lock ( subscriptionLock )
				Subscriptions.Add ( subscription );
		}

		private async Task AddSubscription (
			Message message,
			CryptoExchangeBase exchange,
			decimal threshold,
			IList<CryptoCoinId> coinIds
		)
		{
			var ids = coinIds.ToList ( );

			var sub = UnitOfWork.Get ( unit =>
				{
					var subscription = unit.Subscriptions.Add (
						exchange.Id, message.Chat.Id, message.From.Username, threshold, ids
					);
					foreach (
						var coin
						in
						exchange.ExchangeData.Values.Where ( x => coinIds.Contains ( x.Id ) )
					)
						unit.Subscriptions.UpdateCoin (
							subscription.Id,
							coin.ToCryptoCoinValue ( exchange.Id )
						);
					return subscription;
				}
			);

			StartSubscription ( exchange, sub );

			await SendBlockText (
				message,
				$"Subscribed to {exchange.Name} at a threshold of {threshold:P}\n" +
				$"For coins: {ids.Join ( ", " )}"
			).ConfigureAwait ( false );
		}

		private void LoadSubscriptions ( )
		{
			lock ( subscriptionLock )
				Subscriptions = new List<TelegramSubscription> ( );

			UnitOfWork.Do ( unit =>
				{
					foreach ( var exchange in Exchanges.Values )
					{
						var subscriptions = unit.Subscriptions.GetAll ( exchange.Id );
						foreach ( var subscription in subscriptions.Where ( x => !x.Expired ) )
							StartSubscription ( exchange, subscription );
					}
				}
			);
		}

		private void SendResumeNotifications ( )
		{
			List<IGrouping<long, CryptoExchangeBase>> groups;
			lock ( subscriptionLock )
				groups = Subscriptions.GroupBy ( x => x.ChatId, x => x.Exchange ).ToList ( );
			foreach ( var group in groups )
			{
				var reply = $"Resumed subscription(s) for {group.Select ( x => x.Name ).Join ( ", " )}.";
				Logger.Info ( reply );
				bot.SendTextMessageAsync ( group.Key, $"```\n{reply}\n```", ParseMode.Markdown );
			}
		}

		private static IList<string> BuildCompareTables (
			Dictionary<CryptoExchangeId, Dictionary<CryptoExchangeId, Dictionary<CryptoCoinId, decimal>>> compare )
		{
			var tables = new List<string> ( );

			foreach ( var from in compare )
			{
				var table = new StringBuilder ( );
				var formatter = new TableFormatter ( );
				var objects = new List<IDictionary<string, object>> ( );
				table.AppendLine ( $"{from.Key}" );

				var coinIds = ExtractSymbols ( from.Value.Values );

				foreach ( var value in from.Value )
				{
					var dict = new Dictionary<string, object> {["Exchange"] = $"{value.Key}"};
					foreach ( var id in coinIds )
						dict[id.ToString ( )] =
							value.Value.ContainsKey ( id )
								? $"{value.Value[id]:P}"
								: "";
					objects.Add ( dict );
				}

				table.AppendLine ( formatter.FormatDictionaries ( objects ) );
				table.AppendLine ( );
				tables.Add ( table.ToString ( ) );
			}

			return tables;
		}

		private CryptoExchangeBase GetExchangeBase ( string name ) =>
			Exchanges.Values
				.AsEnumerable ( )
				.FirstOrDefault ( x => x.Name.Equals ( name, StringComparison.CurrentCultureIgnoreCase ) );

		private static IList<CryptoCoinId> ExtractSymbols (
			IEnumerable<Dictionary<CryptoCoinId, decimal>> from
		) =>
			from.Aggregate (
				new List<CryptoCoinId> ( ),
				( current,
				  to ) =>
					current.Union ( to.Keys )
						.OrderBy ( x => x )
						.ToList ( )
			);
	}
}