using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTickerBot;
using CryptoTickerBot.Exchanges;
using CryptoTickerBot.Extensions;
using Newtonsoft.Json;
using Tababular;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Extensions;
using File = System.IO.File;

namespace TelegramBot.CryptoTickerTeleBot
{
	public partial class TeleBot
	{
		private const string SubscriptionFileName = "Subscriptions.json";
		private readonly object subscriptionLock = new object ( );
		private List<CryptoExchangeObserver.ResumableSubscription> subscriptions;

		private void ParseMessage ( Message message, out string command, out List<string> parameters, out string userName )
		{
			var text = message.Text;
			command = text.Split ( ' ' ).First ( );
			if ( command.Contains ( $"@{me.Username}" ) )
				command = command.Substring ( 0, command.IndexOf ( $"@{me.Username}", StringComparison.Ordinal ) );
			parameters = text.Split ( ' ' ).Skip ( 1 ).ToList ( );
			userName   = message.From.Username;
		}

		private async Task<bool> ValidateUserCommand ( string userName, string command, Message message )
		{
			if ( !users.Contains ( userName ) )
			{
				Logger.Info ( $"First message received from {userName}" );
				users.Add ( new TeleBotUser ( userName ) );
			}

			if ( !commands.Keys.Contains ( command ) ) return true;

			if ( Settings.Instance.WhitelistMode && !users.HasUserWithFlag ( userName, UserRole.Registered ) )
			{
				await RequestPurchase ( message, userName );
				return true;
			}

			if ( !users.HasUserWithFlag ( userName, commands[command].role ) )
			{
				await SendBlockText ( message, $"You do not have access to {command}" );
				return true;
			}

			return false;
		}

		private async Task SendBlockText ( Message message, string str )
		{
			await bot.SendTextMessageAsync ( message.Chat.Id, $"```\n{str}\n```", ParseMode.Markdown );
		}

		private async Task RequestPurchase ( Message message, string userName )
		{
			await SendBlockText ( message, $"You need to purchase before you can use this command, {userName}." );
			await bot.SendTextMessageAsync (
				message.Chat.Id,
				$"{Settings.Instance.PurchaseMessageText}"
			);
		}

		private async Task SendSubscriptionReply ( long id, CryptoExchangeBase ex, CryptoCoin oldValue, CryptoCoin newValue )
		{
			var change = newValue - oldValue;
			var builder = new StringBuilder ( );
			builder
				.AppendLine ( $"{ex.Name,-14} {newValue.Symbol}" )
				.AppendLine ( $"Current Price: {ex[newValue.Symbol].Average:C}" )
				.AppendLine ( $"Change:        {change.Value.ToCurrency ( )}" )
				.AppendLine ( $"Change %:      {change.Percentage:P}" )
				.AppendLine ( $"in {change.TimeDiff:dd\\:hh\\:mm\\:ss}" );

			SaveSubscriptions ( );

			await bot.SendTextMessageAsync ( id, $"```\n{builder}\n```", ParseMode.Markdown );
		}

		private async Task AddSubscription ( Message message, CryptoExchange[] chosen, decimal threshold )
		{
			foreach ( var exchange in chosen )
				lock ( subscriptionLock )
				{
					subscriptions.Add (
						ctb.Observers[exchange].Subscribe (
							message.Chat.Id,
							threshold,
							async ( ex, oldValue, newValue ) =>
								await SendSubscriptionReply ( message.Chat.Id, ex, oldValue, newValue ) )
					);
				}

			var reply = $"Subscribed to {chosen.Join ( ", " )} at a threshold of {threshold:P}";
			Logger.Info ( reply );
			await SendBlockText ( message, reply );

			SaveSubscriptions ( );
		}

		private void SaveSubscriptions ( )
		{
			Logger.Debug ( $"Saving subscriptions to {SubscriptionFileName}" );
			lock ( subscriptionLock )
			{
				var json = JsonConvert.SerializeObject ( subscriptions, Formatting.Indented );
				File.WriteAllText ( SubscriptionFileName, json );
			}
		}

		private void LoadSubscriptions ( )
		{
			if ( !File.Exists ( SubscriptionFileName ) )
			{
				SaveSubscriptions ( );
				return;
			}

			Logger.Debug ( $"Loading subscriptions from {SubscriptionFileName}" );
			var json = File.ReadAllText ( SubscriptionFileName );
			lock ( subscriptionLock )
				subscriptions = JsonConvert.DeserializeObject<List<CryptoExchangeObserver.ResumableSubscription>> ( json );
		}

		private void ResumeSubscriptions ( )
		{
			lock ( subscriptionLock )
			{
				foreach ( var subscription in subscriptions )
					ctb.Observers[subscription.Exchange].Subscribe (
						subscription,
						async ( ex, oldValue, newValue ) =>
							await SendSubscriptionReply ( subscription.Id, ex, oldValue, newValue ) );
			}

			List<IGrouping<long, CryptoExchange>> groups;
			lock ( subscriptionLock )
				groups = subscriptions.GroupBy ( x => x.Id, x => x.Exchange ).ToList ( );
			foreach ( var group in groups )
			{
				var reply = $"Resumed subscription(s) for {group.Join ( ", " )}.";
				Logger.Info ( reply );
				bot.SendTextMessageAsync ( group.Key, $"```\n{reply}\n```", ParseMode.Markdown );
			}
		}

		private static IList<string> BuildCompareTables (
			Dictionary<CryptoExchange, Dictionary<CryptoExchange, Dictionary<string, decimal>>> compare )
		{
			var tables = new List<string> ( );

			foreach ( var from in compare )
			{
				var table = new StringBuilder ( );
				var formatter = new TableFormatter ( );
				var objects = new List<IDictionary<string, object>> ( );
				table.AppendLine ( $"{from.Key}" );

				var symbols = ExtractSymbols ( from );

				foreach ( var value in from.Value )
				{
					var dict = new Dictionary<string, object> {["Exchange"] = $"{value.Key}"};
					foreach ( var symbol in symbols )
						dict[symbol] =
							value.Value.ContainsKey ( symbol )
								? $"{value.Value[symbol]:P}"
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
			exchanges.Values
				.AsEnumerable ( )
				.FirstOrDefault ( x => x.Name.Equals ( name, StringComparison.CurrentCultureIgnoreCase ) );

		private static IList<string> ExtractSymbols (
			KeyValuePair<CryptoExchange, Dictionary<CryptoExchange, Dictionary<string, decimal>>> from
		) =>
			from.Value.Values.Aggregate (
				new List<string> ( ),
				( current, to ) =>
					current.Union ( to.Keys )
						.OrderBy ( x => x )
						.ToList ( )
			);
	}
}