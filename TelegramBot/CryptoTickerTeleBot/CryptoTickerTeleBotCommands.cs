using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTickerBot.Exchanges;
using CryptoTickerBot.Extensions;
using Tababular;
using Telegram.Bot.Types;

// ReSharper disable UnusedParameter.Local

namespace TelegramBot.CryptoTickerTeleBot
{
	public partial class TeleBot
	{
		public DateTime StartTime { get; } = DateTime.Now;
		public TimeSpan UpTime => DateTime.Now - StartTime;

		private async Task HandleSubscribe ( Message message, IList<string> @params )
		{
			var chosen = @params
				.Where ( x => GetExchangeBase ( x ) != null )
				.Select ( x => GetExchangeBase ( x ).Id )
				.ToArray ( );

			if ( chosen.Length == 0 )
			{
				chosen = exchanges.Keys.ToArray ( );
				await SendBlockText ( message, "No exchanges provided, subscribing to all exchanges." );
			}

			var threshold = 0.05m;
			var thresholdString = @params.FirstOrDefault ( x => decimal.TryParse ( x.Trim ( '%' ), out var _ ) );
			if ( !string.IsNullOrEmpty ( thresholdString ) )
				threshold = decimal.Parse ( thresholdString ) / 100m;
			else
				await SendBlockText ( message, $"No threshold provided, setting to default {threshold:P}" );

			await AddSubscription ( message, chosen, threshold );
		}

		private async Task HandleUnsubscribe ( Message message, IList<string> _ )
		{
			foreach ( var observer in ctb.Observers.Values )
				observer.Unsubscribe ( message.Chat.Id );

			lock ( subscriptionLock )
				subscriptions.RemoveAll ( x => x.Id == message.Chat.Id );

			SaveSubscriptions ( );

			await SendBlockText ( message, "Unsubscribed from all exchanges." );
		}

		private async Task HandleFetch ( Message message, IList<string> _ )
		{
			var table = exchanges.Values.ToTable ( );
			Logger.Info ( $"Sending ticker data to {message.From.Username}" );

			await SendBlockText ( message, table );
		}

		private async Task HandleCompare ( Message message, IList<string> @params )
		{
			Dictionary<CryptoExchange, Dictionary<CryptoExchange, Dictionary<string, decimal>>> compare;
			if ( @params?.Count >= 2 )
				compare = ctb.CompareTable.Get (
					@params
						.Where ( x => GetExchangeBase ( x ) != null )
						.Select ( x => GetExchangeBase ( x ).Id )
						.ToArray ( )
				);
			else
				compare = ctb.CompareTable.GetAll ( );

			var table = BuildCompareTable ( compare );

			Logger.Info ( $"Sending compare data to {message.From.Username}" );

			await SendBlockText ( message, table.ToString ( ) );
		}

		private async Task HandleBestAll ( Message message )
		{
			var (from, to, first, second, profit) = ctb.CompareTable.GetBest ( );

			if ( first == null || second == null )
			{
				await SendBlockText ( message, "ERROR: Not enough data received." );
				return;
			}

			var fromExchange = exchanges[from];
			var toExchange = exchanges[to];
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
			foreach ( var exchange in exchanges.Values )
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

		private async Task HandleRegister ( Message message, IList<string> userNames )
		{
			foreach ( var userName in userNames )
			{
				if ( string.IsNullOrWhiteSpace ( userName ) )
				{
					await SendBlockText ( message, "Malformed UserName." );
					return;
				}

				Logger.Info ( $"Registered {userName}." );
				users.Add ( new TeleBotUser ( userName, TeleBotUser.Registered ) );
			}

			await SendBlockText ( message, $"Registered {userNames.Join ( ", " )}." );
		}

		private async Task HandleRestart ( Message message, IList<string> _ )
		{
			CryptoTickerBot.Core.Settings.Load ( );
			Settings.Load ( );
			users.Load ( );

			ctb.Stop ( );
			ctb.Start ( );

			await SendBlockText ( message, "Restarted all exchange monitors." );

			while ( !ctb.IsInitialized )
				await Task.Delay ( 10 );

			LoadSubscriptions ( );
			ResumeSubscriptions ( );
		}

		private async Task HandleUsers ( Message message, IList<string> @params )
		{
			if ( @params.Count == 0 )
			{
				foreach ( UserRole value in Enum.GetValues ( typeof ( UserRole ) ) )
					await SendBlockText ( message, $"{value} List:\n{users[value].Select ( x => x.UserName ).Join ( "\n" )}" );

				return;
			}

			if ( !Enum.TryParse ( @params[0], true, out UserRole role ) )
			{
				await SendBlockText ( message,
					$"{@params[0]} is not a known role.\nRoles: {Enum.GetNames ( typeof ( UserRole ) )}" );
				return;
			}

			var list = users[role];
			await SendBlockText ( message, $"{role} List:\n{list.Select ( x => x.UserName ).Join ( "\n" )}" );
		}
	}
}