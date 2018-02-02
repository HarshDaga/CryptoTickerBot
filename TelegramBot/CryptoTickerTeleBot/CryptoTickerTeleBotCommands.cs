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
			var best = ctb.CompareTable.GetBest ( );

			if ( best.first == null || best.second == null )
			{
				await SendBlockText ( message, "ERROR: Not enough data received." );
				return;
			}

			var from = exchanges[best.from];
			var to = exchanges[best.to];
			var fees =
				from.ExchangeData[best.first].Buy ( from.DepositFees[best.first] ) +
				from.ExchangeData[best.first].Sell ( from.WithdrawalFees[best.first] ) +
				to.ExchangeData[best.second].Buy ( to.DepositFees[best.second] ) +
				to.ExchangeData[best.second].Sell ( to.WithdrawalFees[best.second] );
			var minInvestment = fees / best.profit;

			var reply =
				$"Buy  {best.first} From: {from.Name,-12} @ {from[best.first].BuyPrice:C}\n" +
				$"Sell {best.first} To:   {to.Name,-12} @ {to[best.first].SellPrice:C}\n" +
				$"Buy  {best.second} From: {to.Name,-12} @ {to[best.second].BuyPrice:C}\n" +
				$"Sell {best.second} To:   {from.Name,-12} @ {from[best.second].SellPrice:C}\n" +
				$"Expected profit:    {best.profit:P}\n" +
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
			var objects = new List<object> ( );
			foreach ( var exchange in exchanges.Values )
				objects.Add ( new
				{
					Exchange   = exchange.Name,
					UpTime     = $"{exchange.UpTime:hh\\:mm\\:ss}",
					LastUpdate = $"{exchange.Age:hh\\:mm\\:ss}",
					LastChange = $"{exchange.LastChangeDuration:hh\\:mm\\:ss}"
				} );

			var builder = new StringBuilder ( );
			builder
				.AppendLine ( $"Running since {UpTime:dd\\:hh\\:mm\\:ss}" )
				.AppendLine ( "" )
				.AppendLine ( formatter.FormatObjects ( objects ) );

			await SendBlockText ( message, builder.ToString ( ) );
		}

		private async Task HandleWhitelist ( Message message, IList<string> userNames )
		{
			foreach ( var userName in userNames )
			{
				if ( string.IsNullOrWhiteSpace ( userName ) )
				{
					await SendBlockText ( message, "Malformed UserName." );
					return;
				}

				Logger.Info ( $"Registered {userName}." );
				users.Add ( new TeleBotUser ( userName, UserRole.Registered | UserRole.Guest ) );
			}

			await SendBlockText ( message, $"Registered {userNames.Join ( ", " )}." );
		}

		private async Task HandleRestart ( Message message, IList<string> _ )
		{
			ctb.Stop ( );
			ctb.Start ( );

			await SendBlockText ( message, "Restarted all exchange monitors." );

			while ( !ctb.IsInitialized )
				await Task.Delay ( 10 );

			LoadSubscriptions ( );
			ResumeSubscriptions ( );
		}
	}
}