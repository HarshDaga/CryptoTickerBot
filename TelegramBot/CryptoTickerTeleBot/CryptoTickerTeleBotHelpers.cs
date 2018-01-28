using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using CryptoTickerBot.Exchanges;
using CryptoTickerBot.Extensions;
using Newtonsoft.Json;
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

		private async Task SendBlockText ( Message message, string str )
		{
			await bot.SendTextMessageAsync ( message.Chat.Id, $"```\n{str}\n```", ParseMode.Markdown );
		}

		private async Task AddSubscription ( Message message, CryptoExchange[] chosen, decimal threshold )
		{
			foreach ( var exchange in chosen )
				lock ( subscriptionLock )
				{
					subscriptions.Add (
						ctb.Observers[exchange].Subscribe ( message.Chat.Id, threshold,
							async ( ex, oldValue, newValue ) =>
							{
								var change = newValue - oldValue;
								var builder = new StringBuilder ( );
								builder
									.AppendLine ( $"{ex.Name,-10}" )
									.AppendLine ( $"Current Price: {ex[newValue.Symbol].Average:C}" )
									.AppendLine ( $"Change: {change.Value.ToCurrency ( ),-8} {change.Percentage,6:P}" )
									.AppendLine ( $"in {change.TimeDiff:dd\\:hh\\:mm\\:ss}" );

								await SendBlockText ( message, builder.ToString ( ) );
							} )
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

			Task.Run ( ( ) =>
			{
				var timer = new Timer ( 1000 * 60 * 10 );
				timer.Elapsed += ( sender, args ) => SaveSubscriptions ( );
				timer.Start ( );
			} );
		}

		private void ResumeSubscriptions ( )
		{
			lock ( subscriptionLock )
			{
				foreach ( var subscription in subscriptions )
					ctb.Observers[subscription.Exchange].Subscribe ( subscription,
						async ( ex, oldValue, newValue ) =>
						{
							var change = newValue - oldValue;
							var builder = new StringBuilder ( );
							builder
								.AppendLine ( $"{ex.Name,-10}" )
								.AppendLine ( $"Current Price: {ex[newValue.Symbol].Average:C}" )
								.AppendLine ( $"Change: {change.Value.ToCurrency ( ),-8} {change.Percentage,6:P}" )
								.AppendLine ( $"in {change.TimeDiff:dd\\:hh\\:mm\\:ss}" );

							await bot.SendTextMessageAsync ( subscription.Id, $"```\n{builder}\n```", ParseMode.Markdown );
						} );
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

		private static StringBuilder BuildCompareTable (
			Dictionary<CryptoExchange, Dictionary<CryptoExchange, Dictionary<string, decimal>>> compare )
		{
			var table = new StringBuilder ( );
			foreach ( var from in compare )
			{
				table.AppendLine ( $"{from.Key}" );
				table.AppendLine ( $"{"Symbol",-8}{from.Value.Keys.Select ( x => $"{x,-10}" ).Join ( "" )}" );

				var symbols = ExtractSymbols ( from );

				foreach ( var symbol in symbols )
				{
					table.Append ( $"{symbol,-8}" );
					foreach ( var value in from.Value )
						table.Append (
							value.Value.ContainsKey ( symbol )
								? $"{value.Value[symbol],-10:P}"
								: $"{"",-10}"
						);
					table.AppendLine ( );
				}

				table.AppendLine ( );
			}

			return table;
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