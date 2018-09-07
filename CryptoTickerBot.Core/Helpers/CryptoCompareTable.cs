using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Domain;
using MoreLinq;

namespace CryptoTickerBot.Core.Helpers
{
	using CoinMap = Dictionary<string, decimal>;
	using ExchangeToCoinMap = Dictionary<CryptoExchangeId, Dictionary<string, decimal>>;

	public class CryptoCompareTable
	{
		public Dictionary<CryptoExchangeId, ICryptoExchange> Exchanges { get; } =
			new Dictionary<CryptoExchangeId, ICryptoExchange> ( );

		public CryptoCompareTable ( params ICryptoExchange[] exchanges )
		{
			foreach ( var exchange in exchanges )
				AddExchange ( exchange );
		}

		[DebuggerStepThrough]
		public void AddExchange ( ICryptoExchange exchange ) =>
			Exchanges[exchange.Id] = exchange;

		[Pure]
		public CoinMap GetPair ( CryptoExchangeId from,
		                         CryptoExchangeId to )
		{
			if ( !Exchanges.ContainsKey ( from ) || !Exchanges.ContainsKey ( to ) )
				return null;

			var result = new CoinMap ( );
			var symbols =
				Exchanges[from].ExchangeData.Keys
					.Intersect ( Exchanges[to].ExchangeData.Keys )
					.ToList ( );

			foreach ( var symbol in symbols )
			{
				var buy = Exchanges[from].ExchangeData[symbol].BuyPrice;
				var sell = Exchanges[to].ExchangeData[symbol].SellPrice;

				if ( buy == 0 || sell == 0 )
					continue;

				result[symbol] = ( sell - buy ) / buy;
			}

			return result;
		}

		[Pure]
		public ExchangeToCoinMap GetAll ( CryptoExchangeId from )
		{
			if ( !Exchanges.ContainsKey ( from ) )
				return null;

			var result = new ExchangeToCoinMap ( );
			foreach ( var to in Exchanges.Keys )
				result[to] = GetPair ( from, to );

			return result;
		}

		[Pure]
		public Dictionary<CryptoExchangeId, ExchangeToCoinMap> GetAll ( )
		{
			var result = new Dictionary<CryptoExchangeId, ExchangeToCoinMap> ( );
			foreach ( var exchange in Exchanges.Keys )
				result[exchange] = GetAll ( exchange );

			return result;
		}

		[Pure]
		public Dictionary<CryptoExchangeId, ExchangeToCoinMap> Get ( params CryptoExchangeId[] exchanges )
		{
			var result = new Dictionary<CryptoExchangeId, ExchangeToCoinMap> ( );
			foreach ( var exchange in exchanges.Intersect ( Exchanges.Keys ) )
				result[exchange] = GetAll ( exchange )
					.Where ( pair => exchanges.Contains ( pair.Key ) )
					.ToDictionary ( );

			return result;
		}

		public static void RemoveExchange (
			ExchangeToCoinMap compare,
			params CryptoExchangeId[] cryptoExchanges
		)
		{
			foreach ( var exchange in cryptoExchanges )
				compare.Remove ( exchange );
		}

		public static void RemoveExchange (
			Dictionary<CryptoExchangeId, ExchangeToCoinMap> compare,
			params CryptoExchangeId[] cryptoExchanges
		)
		{
			foreach ( var exchange in cryptoExchanges )
				compare.Remove ( exchange );

			foreach ( var compareValue in compare.Values )
				RemoveExchange ( compareValue, cryptoExchanges );
		}

		[Pure]
		public static (string best, string leastWorst, decimal profit) GetBestPair (
			Dictionary<CryptoExchangeId, ExchangeToCoinMap> compare,
			CryptoExchangeId from,
			CryptoExchangeId to
		)
		{
			var best = compare[from][to].MaxBy ( x => x.Value ).Last ( ).Key;
			var leastWorst = compare[to][from].MaxBy ( x => x.Value ).Last ( ).Key;
			var profit =
				( 1m + compare[from][to][best] )
				* ( 1m + compare[to][from][leastWorst] )
				- 1m;

			return ( best, leastWorst, profit );
		}

		[Pure]
		public (string best, string leastWorst, decimal profit, decimal fees) GetBestPair (
			CryptoExchangeId from,
			CryptoExchangeId to
		)
		{
			var compare = GetAll ( );
			var (best, leastWorst, profit) = GetBestPair ( compare, from, to );
			var fromExchange = Exchanges[from];
			var toExchange = Exchanges[to];

			var fees =
				fromExchange[best].Buy ( fromExchange.DepositFees[best] ) +
				fromExchange[best].Sell ( fromExchange.WithdrawalFees[best] ) +
				toExchange[leastWorst].Buy ( toExchange.DepositFees[leastWorst] ) +
				toExchange[leastWorst].Sell ( toExchange.WithdrawalFees[leastWorst] );

			return ( best, leastWorst, profit, fees );
		}

		[Pure]
		public
			(
			CryptoExchangeId from,
			CryptoExchangeId to,
			string first,
			string second,
			decimal profit,
			decimal fees
			)
			GetBest ( )
		{
			var all = GetAll ( );
			var exchanges = Exchanges.Keys.ToList ( );
			var bestGain = decimal.MinValue;
			( CryptoExchangeId from, CryptoExchangeId to, string first, string second, decimal profit,
				decimal fees )
				result = default;

			foreach ( var from in exchanges )
			foreach ( var to in exchanges )
			{
				if ( from == to || all[from][to].Count == 0 || all[to][from].Count == 0 )
					continue;

				var (best, leastWorst, profit) = GetBestPair ( all, from, to );
				if ( profit > bestGain )
				{
					bestGain = profit;
					result   = ( from, to, best, leastWorst, profit, 0 );
				}
			}

			var fromExchange = Exchanges[result.from];
			var toExchange = Exchanges[result.to];
			result.fees =
				fromExchange[result.first].Buy ( fromExchange.DepositFees[result.first] ) +
				fromExchange[result.first].Sell ( fromExchange.WithdrawalFees[result.first] ) +
				toExchange[result.second].Buy ( toExchange.DepositFees[result.second] ) +
				toExchange[result.second].Sell ( toExchange.WithdrawalFees[result.second] );

			return result;
		}
	}
}