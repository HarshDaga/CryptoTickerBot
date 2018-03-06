using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core;
using CryptoTickerBot.Data;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Persistence;
using CryptoTickerBot.Extensions;
using CryptoTickerBot.Helpers;
using Tababular;
using Telegram.Bot.Types;
using TelegramBot.Extensions;

// ReSharper disable UnusedParameter.Local

namespace TelegramBot.CryptoTickerTeleBot
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
				await SendBlockText ( message, "No exchanges provided." );
				await SendBlockText ( message,
				                      "Supported Exchanges:\n" +
				                      $"{Exchanges.Select ( e => e.Value.Name ).Join ( "\n" )}"
				);
				return;
			}

			// Check threshold value
			var threshold = 0.05m;
			var thresholdString = @params.FirstOrDefault ( x => decimal.TryParse ( x.Trim ( '%' ), out var _ ) );
			if ( !string.IsNullOrEmpty ( thresholdString ) )
				threshold = decimal.Parse ( thresholdString ) / 100m;
			else
				await SendBlockText ( message, $"No threshold provided, setting to default {threshold:P}" );

			// Check coin symbols
			var supported = Bot.SupportedCoins;
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
				);
				coinIds = supported.Select ( x => x.Id ).ToList ( );
			}

			await AddSubscription ( message, chosen, threshold, coinIds );
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

			await SendBlockText ( message, "Unsubscribed from all exchanges." );
		}

		private async Task HandleFetch ( Message message, IList<string> @params )
		{
			var fiat = FiatCurrency.USD;
			if ( @params.Count >= 1 )
				fiat = @params[0].ToFiatCurrency ( );

			var tables = Exchanges.Values.ToTables ( fiat );
			Logger.Info ( $"Sending ticker data to {message.From.Username}" );

			foreach ( var table in tables )
				await SendBlockText ( message, table );
		}

		private async Task HandleCompare ( Message message, IList<string> @params )
		{
			Dictionary<CryptoExchangeId, Dictionary<CryptoExchangeId, Dictionary<CryptoCoinId, decimal>>> compare;
			if ( @params?.Count >= 2 )
				compare = ctb.CompareTable.Get (
					@params
						.Where ( x => GetExchangeBase ( x ) != null )
						.Select ( x => GetExchangeBase ( x ).Id )
						.ToArray ( )
				);
			else
				compare = ctb.CompareTable.GetAll ( );

			var tables = BuildCompareTables ( compare );

			Logger.Info ( $"Sending compare data to {message.From.Username}" );

			foreach ( var table in tables )
				await SendBlockText ( message, table );
		}

		private async Task HandleBestAll ( Message message )
		{
			var (from, to, first, second, profit) = ctb.CompareTable.GetBest ( );

			if ( first == CryptoCoinId.NULL || second == CryptoCoinId.NULL )
			{
				await SendBlockText ( message, "ERROR: Not enough data received." );
				return;
			}

			var fromExchange = Exchanges[from];
			var toExchange = Exchanges[to];
			var fees =
				fromExchange.ExchangeData[first].Buy ( fromExchange.DepositFees[first] ) +
				fromExchange.ExchangeData[first].Sell ( fromExchange.WithdrawalFees[first] ) +
				toExchange.ExchangeData[second].Buy ( toExchange.DepositFees[second] ) +
				toExchange.ExchangeData[second].Sell ( toExchange.WithdrawalFees[second] );
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
			await SendBlockText ( message, reply );
		}

		private async Task HandleBest ( Message message, IList<string> @params )
		{
			if ( @params == null || @params.Count < 2 )
			{
				await HandleBestAll ( message );
				return;
			}

			var from = GetExchangeBase ( @params[0] );
			var to = GetExchangeBase ( @params[1] );

			if ( from == null )
			{
				await SendBlockText ( message, $"ERROR: {@params[0]} not found." );
				return;
			}

			if ( to == null )
			{
				await SendBlockText ( message, $"ERROR: {@params[1]} not found." );
				return;
			}

			if ( from.Count == 0 || to.Count == 0 )
			{
				await SendBlockText ( message, "ERROR: Not enough data received." );
				return;
			}

			var (best, leastWorst, profit) = ctb.CompareTable.GetBestPair ( from.Id, to.Id );
			var fees =
				from.ExchangeData[best].Buy ( from.DepositFees[best] ) +
				from.ExchangeData[best].Sell ( from.WithdrawalFees[best] ) +
				to.ExchangeData[leastWorst].Buy ( to.DepositFees[leastWorst] ) +
				to.ExchangeData[leastWorst].Sell ( to.WithdrawalFees[leastWorst] );
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
			await SendBlockText ( message, reply );
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

			await SendBlockText ( message, builder.ToString ( ) );
		}

		private async Task HandlePutGroup ( Message message, IList<string> @params )
		{
			if ( @params.Count < 2 )
			{
				await SendBlockText (
					message,
					"Not enough arguments.\n" +
					"Syntax: /putgroup <Role Name> <Usernames CSV>" );
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
				);
				return;
			}

			foreach ( var userName in @params.Skip ( 1 ) )
			{
				if ( string.IsNullOrWhiteSpace ( userName ) )
				{
					await SendBlockText ( message, "Malformed UserName." );
					return;
				}

				Logger.Info ( $"Registered {userName}." );

				var user = new TeleBotUser ( userName, role );
				Users.AddOrUpdate ( user );
				UnitOfWork.Do ( unit => unit.Users.AddOrUpdate ( user.UserName, user.Role ) );
			}

			await SendBlockText ( message, $"Registered {@params.Join ( ", " )}." );
		}

		private async Task HandleRestart ( Message message, IList<string> _ )
		{
			Settings.Load ( );
			FetchUserList ( );

			ctb.Stop ( );
			ctb = Bot.CreateAndStart (
				new CancellationTokenSource ( ),
				Settings.Instance.ApplicationName,
				Settings.Instance.SheetName,
				Settings.Instance.SheetId,
				Settings.Instance.SheetsRanges
			);

			await SendBlockText ( message, "Restarted all exchange monitors." );

			while ( !ctb.IsInitialized )
				await Task.Delay ( 10 );

			LoadSubscriptions ( );
			SendResumeNotifications ( );
		}

		private async Task HandleUsers ( Message message, IList<string> @params )
		{
			if ( @params.Count == 0 )
			{
				foreach ( UserRole value in Enum.GetValues ( typeof ( UserRole ) ) )
					await SendBlockText (
						message,
						$"{value} List:\n{Users.OfRole ( value ).Select ( x => x.UserName ).Join ( "\n" )}"
					);

				return;
			}

			if ( !Enum.TryParse ( @params[0], true, out UserRole role ) )
			{
				await SendBlockText (
					message,
					$"{@params[0]} is not a known role.\nRoles: {Enum.GetNames ( typeof ( UserRole ) )}"
				);
				return;
			}

			var list = Users.OfRole ( role );
			await SendBlockText ( message, $"{role} List:\n{list.Select ( x => x.UserName ).Join ( "\n" )}" );
		}

		private async Task HandleKill ( Message message, IList<string> @params )
		{
			Logger.Info ( $"Shutting down per {message.From.Username}'s request." );
			await SendBlockText ( message, "Bye Bye 👋🏻👋🏻" );
			await Task.Delay ( 100 );
			Environment.Exit ( 0 );
		}
	}
}