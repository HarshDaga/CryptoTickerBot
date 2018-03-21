using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Extensions;
using CryptoTickerBot.Data.Persistence;
using CryptoTickerBot.Extensions;
using CryptoTickerBot.Helpers;
using Tababular;
using Telegram.Bot.Types;
using TelegramBot.Extensions;

// ReSharper disable UnusedParameter.Local

namespace TelegramBot.Core
{
	public partial class TeleBot
	{
		public DateTime StartTime { get; } = DateTime.UtcNow;
		public TimeSpan UpTime => DateTime.UtcNow - StartTime;

		private async Task HandleSubscribe ( Message message, IList<string> @params )
		{
			// Check exchange name
			var chosen = @params
				.Where ( x => GetExchangeBase ( x ) != null )
				.Select ( GetExchangeBase )
				.FirstOrDefault ( );

			if ( chosen == null )
			{
				await SendBlockText ( message, "No exchanges provided." ).ConfigureAwait ( false );
				await SendBlockText ( message,
				                      "Supported Exchanges:\n" +
				                      $"{Exchanges.Select ( e => e.Value.Name ).Join ( "\n" )}"
				).ConfigureAwait ( false );
				return;
			}

			// Check threshold value
			var threshold = 0.05m;
			var thresholdString = @params
				.FirstOrDefault ( x => decimal.TryParse ( x.Trim ( '%' ), out var _ ) );
			if ( !string.IsNullOrEmpty ( thresholdString ) )
				threshold = decimal.Parse ( thresholdString ) / 100m;
			else
				await SendBlockText ( message, $"No threshold provided, setting to default {threshold:P}" )
					.ConfigureAwait ( false );

			// Check coin symbols
			var supported = CryptoTickerBotCore.SupportedCoins;
			var coinIds = supported
				.Where ( x => @params.Any (
					         c => c.Equals ( x.Symbol, StringComparison.InvariantCultureIgnoreCase )
				         )
				)
				.Select ( x => x.Id )
				.ToList ( );
			if ( coinIds.Count == 0 )
			{
				await SendBlockText (
					message,
					"No coin symbols provided. Subscribing to all coin notifications."
				).ConfigureAwait ( false );
				coinIds = supported.Select ( x => x.Id ).ToList ( );
			}

			await AddSubscription ( message, chosen, threshold, coinIds ).ConfigureAwait ( false );
		}

		private async Task HandleUnsubscribe ( Message message, IList<string> _ )
		{
			lock ( subscriptionLock )
			{
				foreach ( var subscription in Subscriptions.Where ( x => x.ChatId == message.Chat.Id ) )
					subscription.Dispose ( );

				Subscriptions.RemoveAll ( x => x.ChatId == message.Chat.Id );
			}

			UnitOfWork.Do ( u => u.Subscriptions.Remove ( message.Chat.Id ) );

			await SendBlockText ( message, "Unsubscribed from all exchanges." ).ConfigureAwait ( false );
		}

		private async Task HandleFetch ( Message message, IList<string> @params )
		{
			var fiat = FiatCurrency.USD;
			if ( @params.Count >= 1 )
				fiat = @params[0].ToFiatCurrency ( );

			var tables = Exchanges.Values.ToTables ( fiat );
			Logger.Info ( $"Sending ticker data to {message.From.Username}" );

			foreach ( var table in tables )
				await SendBlockText ( message, table ).ConfigureAwait ( false );
		}

		private async Task HandleCompare ( Message message, IList<string> @params )
		{
			Dictionary<CryptoExchangeId, Dictionary<CryptoExchangeId, Dictionary<CryptoCoinId, decimal>>> compare;
			if ( @params?.Count >= 2 )
				compare = Ctb.CompareTable.Get (
					@params
						.Where ( x => GetExchangeBase ( x ) != null )
						.Select ( x => GetExchangeBase ( x ).Id )
						.ToArray ( )
				);
			else
				compare = Ctb.CompareTable.GetAll ( );

			var tables = BuildCompareTables ( compare );

			Logger.Info ( $"Sending compare data to {message.From.Username}" );

			foreach ( var table in tables )
				await SendBlockText ( message, table ).ConfigureAwait ( false );
		}

		private async Task HandleBestAll ( Message message )
		{
			var (from, to, first, second, profit, fees) = Ctb.CompareTable.GetBest ( );

			if ( first == CryptoCoinId.NULL || second == CryptoCoinId.NULL )
			{
				await SendBlockText ( message, "ERROR: Not enough data received." ).ConfigureAwait ( false );
				return;
			}

			var fromExchange = Exchanges[from];
			var toExchange = Exchanges[to];
			var minInvestment = fees / profit;

			var reply =
				$"Buy  {first} From: {fromExchange.Name,-12} @ {fromExchange[first].BuyPrice:C}\n" +
				$"Sell {first} To:   {toExchange.Name,-12} @ {toExchange[first].SellPrice:C}\n" +
				$"Buy  {second} From: {toExchange.Name,-12} @ {toExchange[second].BuyPrice:C}\n" +
				$"Sell {second} To:   {fromExchange.Name,-12} @ {fromExchange[second].SellPrice:C}\n" +
				$"Expected profit:    {profit:P}\n" +
				$"Estimated fees:     {fees:C}\n" +
				$"Minimum Investment: {minInvestment:C}";

			Logger.Info ( $"Sending best pair data to {message.From.Username}" );
			await SendBlockText ( message, reply ).ConfigureAwait ( false );
		}

		private async Task HandleBest ( Message message, IList<string> @params )
		{
			if ( @params == null || @params.Count < 2 )
			{
				await HandleBestAll ( message ).ConfigureAwait ( false );
				return;
			}

			var from = GetExchangeBase ( @params[0] );
			var to = GetExchangeBase ( @params[1] );

			if ( from == null )
			{
				await SendBlockText ( message, $"ERROR: {@params[0]} not found." ).ConfigureAwait ( false );
				return;
			}

			if ( to == null )
			{
				await SendBlockText ( message, $"ERROR: {@params[1]} not found." ).ConfigureAwait ( false );
				return;
			}

			if ( from.Count == 0 || to.Count == 0 )
			{
				await SendBlockText ( message, "ERROR: Not enough data received." ).ConfigureAwait ( false );
				return;
			}

			var (best, leastWorst, profit, fees) = Ctb.CompareTable.GetBestPair ( from.Id, to.Id );
			var minInvestment = fees / profit;

			var reply =
				$"Buy  {best} From: {from.Name,-12} @ {from[best].BuyPrice:C}\n" +
				$"Sell {best} To:   {to.Name,-12} @ {to[best].SellPrice:C}\n" +
				$"Buy  {leastWorst} From: {to.Name,-12} @ {to[leastWorst].BuyPrice:C}\n" +
				$"Sell {leastWorst} To:   {from.Name,-12} @ {from[leastWorst].SellPrice:C}\n" +
				$"Expected profit:    {profit:P}\n" +
				$"Estimated fees:     {fees:C}\n" +
				$"Minimum Investment: {minInvestment:C}";

			Logger.Info ( $"Sending best pair data to {message.From.Username}" );
			await SendBlockText ( message, reply ).ConfigureAwait ( false );
		}

		private async Task HandleStatus ( Message message, IList<string> _ )
		{
			var formatter = new TableFormatter ( );
			var objects = new List<IDictionary<string, string>> ( );
			foreach ( var exchange in Exchanges.Values )
				objects.Add ( new Dictionary<string, string>
				{
					["Exchange"]    = exchange.Name,
					["Up Time"]     = $"{exchange.UpTime:hh\\:mm\\:ss}",
					["Last Update"] = $"{exchange.Age:hh\\:mm\\:ss}",
					["Last Change"] = $"{exchange.LastChangeDuration:hh\\:mm\\:ss}"
				} );

			var builder = new StringBuilder ( );
			builder
				.AppendLine ( $"Running since {UpTime:dd\\:hh\\:mm\\:ss}" )
				.AppendLine ( "" )
				.AppendLine ( formatter.FormatDictionaries ( objects ) );

			await SendBlockText ( message, builder.ToString ( ) ).ConfigureAwait ( false );
		}

		private async Task HandlePutGroup ( Message message, IList<string> @params )
		{
			if ( @params.Count < 2 )
			{
				await SendBlockText (
					message,
					"Not enough arguments.\n" +
					"Syntax: /putgroup <Role Name> <User ID> <Username>"
				).ConfigureAwait ( false );
				return;
			}

			UserRole role;
			try
			{
				role = @params[0].ToEnum<UserRole> ( );
			}
			catch
			{
				await SendBlockText (
					message,
					$"Unknown role {@params[0]}.\n" +
					$"Roles: {Enum.GetNames ( typeof ( UserRole ) ).Join ( ", " )}"
				).ConfigureAwait ( false );
				return;
			}

			if ( !int.TryParse ( @params[1], out var id ) )
			{
				await SendBlockText (
					message,
					$"Invalid ID {@params[1]}.\n" +
					"ID must be an integer."
				).ConfigureAwait ( false );
				return;
			}


			var tbu = UnitOfWork.Get ( unit => unit.Users.Get ( id ) );
			TelegramBotUser user = tbu ?? new TelegramBotUser ( id );
			user.Role = role;
			Users.AddOrUpdate ( user );
			UnitOfWork.Do ( unit => unit.Users.AddOrUpdate ( user ) );

			Logger.Info ( $"Registered {user}" );
			await SendBlockText ( message, $"Registered {user}." ).ConfigureAwait ( false );
		}

		private async Task HandleRestart ( Message message, IList<string> _ )
		{
			Settings.Load ( );
			FetchUserList ( );

			Ctb.Stop ( );
			Ctb = CryptoTickerBotCore.CreateAndStart ( new CancellationTokenSource ( ) );

			await SendBlockText ( message, "Restarted all exchange monitors." ).ConfigureAwait ( false );

			while ( !Ctb.IsInitialized )
				await Task.Delay ( 10 ).ConfigureAwait ( false );

			LoadSubscriptions ( );
			SendResumeNotifications ( );

			Restart?.Invoke ( this );
		}

		private async Task HandleUsers ( Message message, IList<string> @params )
		{
			if ( @params.Count == 0 )
			{
				foreach ( UserRole value in Enum.GetValues ( typeof ( UserRole ) ) )
				{
					var query = Users
						.OfRole ( value )
						.OrderBy ( x => x.Created )
						.Select ( x => $"{x.Id,-10} {x.UserName}" );
					await SendBlockText (
						message,
						$"{value} List:\n{query.Join ( "\n" )}"
					).ConfigureAwait ( false );
				}

				return;
			}

			if ( !Enum.TryParse ( @params[0], true, out UserRole role ) )
			{
				await SendBlockText (
					message,
					$"{@params[0]} is not a known role.\nRoles: {Enum.GetNames ( typeof ( UserRole ) )}"
				).ConfigureAwait ( false );
				return;
			}

			var list = Users
				.OfRole ( role )
				.OrderBy ( x => x.Created )
				.Select ( x => $"{x.Id,-10} {x.UserName}" );

			await SendBlockText (
				message,
				$"{role} List:\n{list.Join ( "\n" )}"
			).ConfigureAwait ( false );
		}

		private async Task HandleKill ( Message message, IList<string> @params )
		{
			Logger.Info ( $"Shutting down per {message.From.Username}'s request." );
			await SendBlockText ( message, "Bye Bye 👋🏻👋🏻" ).ConfigureAwait ( false );
			await Task.Delay ( 100 ).ConfigureAwait ( false );
			Environment.Exit ( 0 );
		}
	}
}