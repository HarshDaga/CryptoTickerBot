using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTickerBot.Extensions;
using Telegram.Bot.Types;

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

		private async Task HandleUnsubscribe ( Message message )
		{
			foreach ( var observer in ctb.Observers.Values )
				observer.Unsubscribe ( message.Chat.Id );

			lock ( subscriptionLock )
				subscriptions.RemoveAll ( x => x.Id == message.Chat.Id );

			SaveSubscriptions ( );

			await SendBlockText ( message, "Unsubscribed from all exchanges." );
		}

		private async Task HandleFetch ( Message message )
		{
			var table = exchanges.Values.ToTable ( );
			Logger.Info ( $"Sending ticker data to {message.From.Username}" );

			await SendBlockText ( message, table );
		}

		private async Task HandleCompare ( Message message, IEnumerable<string> @params )
		{
			var compare = ctb.CompareTable.Get (
				@params
					.Where ( x => GetExchangeBase ( x ) != null )
					.Select ( x => GetExchangeBase ( x ).Id )
					.ToArray ( )
			);

			var table = BuildCompareTable ( compare );

			Logger.Info ( $"Sending compare data to {message.From.Username}" );

			await SendBlockText ( message, table.ToString ( ) );
		}

		private async Task HandleCompare ( Message message )
		{
			var compare = ctb.CompareTable.GetAll ( );

			var table = BuildCompareTable ( compare );

			Logger.Info ( $"Sending compare data to {message.From.Username}" );

			await SendBlockText ( message, table.ToString ( ) );
		}

		private async Task HandleBest ( Message message )
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
			if ( @params.Count < 2 )
				return;

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

		private async Task HandleStatus ( Message message )
		{
			var builder = new StringBuilder ( );
			builder
				.AppendLine ( $"Running since {UpTime:dd\\:hh\\:mm\\:ss}" )
				.AppendLine ( "Exchanges:" );
			foreach ( var exchange in exchanges.Values )
				builder.AppendLine (
					$"{exchange.Name,-10} Since {exchange.UpTime:dd\\:hh\\:mm\\:ss} Last Update {exchange.Age:dd\\:hh\\:mm\\:ss}" );

			await SendBlockText ( message, builder.ToString ( ) );
		}
	}
}