using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Exchanges.Core;
using MoreLinq;

namespace CryptoTickerBot.Helpers
{
	public class CryptoCompareTable
	{
		public Dictionary<CryptoExchangeId, CryptoExchangeBase> Exchanges { get; } =
			new Dictionary<CryptoExchangeId, CryptoExchangeBase> ( );

		public CryptoCompareTable ( params CryptoExchangeBase[] exchanges )
		{
			foreach ( var exchange in exchanges )
				AddExchange ( exchange );
		}

		[DebuggerStepThrough]
		public void AddExchange ( CryptoExchangeBase exchange ) =>
			Exchanges[exchange.Id] = exchange;

		public Dictionary<CryptoCoinId, decimal> GetPair ( CryptoExchangeId from, CryptoExchangeId to )
		{
			if ( !Exchanges.ContainsKey ( from ) || !Exchanges.ContainsKey ( to ) )
				return null;

			var result = new Dictionary<CryptoCoinId, decimal> ( );
			var symbols =
				Exchanges[from].ExchangeData.Keys
					.Intersect ( Exchanges[to].ExchangeData.Keys )
					.ToList ( );

			foreach ( var symbol in symbols )
			{
				var buy = Exchanges[from].ExchangeData[symbol].BuyPrice;
				var sell = Exchanges[to].ExchangeData[symbol].SellPrice;
				result[symbol] = ( sell - buy ) / buy;
			}

			return result;
		}

		public Dictionary<CryptoExchangeId, Dictionary<CryptoCoinId, decimal>> GetAll ( CryptoExchangeId from )
		{
			if ( !Exchanges.ContainsKey ( from ) )
				return null;

			var result = new Dictionary<CryptoExchangeId, Dictionary<CryptoCoinId, decimal>> ( );
			foreach ( var to in Exchanges.Keys )
				result[to] = GetPair ( from, to );

			return result;
		}

		public Dictionary<CryptoExchangeId, Dictionary<CryptoExchangeId, Dictionary<CryptoCoinId, decimal>>> GetAll ( )
		{
			var result = new Dictionary<CryptoExchangeId, Dictionary<CryptoExchangeId, Dictionary<CryptoCoinId, decimal>>> ( );
			foreach ( var exchange in Exchanges.Keys )
				result[exchange] = GetAll ( exchange );

			return result;
		}

		public Dictionary<CryptoExchangeId, Dictionary<CryptoExchangeId, Dictionary<CryptoCoinId, decimal>>> Get (
			params CryptoExchangeId[] exchanges
		)
		{
			var result = new Dictionary<CryptoExchangeId, Dictionary<CryptoExchangeId, Dictionary<CryptoCoinId, decimal>>> ( );
			foreach ( var exchange in exchanges.Intersect ( Exchanges.Keys ) )
				result[exchange] = GetAll ( exchange )
					.Where ( pair => exchanges.Contains ( pair.Key ) )
					.ToDictionary ( );

			return result;
		}

		public static void RemoveExchange (
			Dictionary<CryptoExchangeId, Dictionary<string, decimal>> compare,
			params CryptoExchangeId[] cryptoExchanges
		)
		{
			foreach ( var exchange in cryptoExchanges )
				compare.Remove ( exchange );
		}

		public static void RemoveExchange (
			Dictionary<CryptoExchangeId, Dictionary<CryptoExchangeId, Dictionary<string, decimal>>> compare,
			params CryptoExchangeId[] cryptoExchanges
		)
		{
			foreach ( var exchange in cryptoExchanges )
				compare.Remove ( exchange );

			foreach ( var compareValue in compare.Values )
				RemoveExchange ( compareValue, cryptoExchanges );
		}

		public static (CryptoCoinId best, CryptoCoinId leastWorst, decimal profit) GetBestPair (
			Dictionary<CryptoExchangeId, Dictionary<CryptoExchangeId, Dictionary<CryptoCoinId, decimal>>> compare,
			CryptoExchangeId from,
			CryptoExchangeId to
		)
		{
			var best = compare[from][to].MaxBy ( x => x.Value ).Key;
			var leastWorst = compare[to][from].MaxBy ( x => x.Value ).Key;
			var profit =
				( 1m + compare[from][to][best] )
				* ( 1m + compare[to][from][leastWorst] )
				- 1m;

			return ( best, leastWorst, profit );
		}

		public (CryptoCoinId best, CryptoCoinId leastWorst, decimal profit) GetBestPair (
			CryptoExchangeId from, CryptoExchangeId to )
		{
			var compare = GetAll ( );
			return GetBestPair ( compare, from, to );
		}

		public
			(
			CryptoExchangeId from,
			CryptoExchangeId to,
			CryptoCoinId first,
			CryptoCoinId second,
			decimal profit
			)
			GetBest ( )
		{
			var all = GetAll ( );
			var exchanges = Exchanges.Keys.ToList ( );
			var bestGain = decimal.MinValue;
			(CryptoExchangeId from, CryptoExchangeId to, CryptoCoinId first, CryptoCoinId second, decimal profit) result =
				default;

			foreach ( var from in exchanges )
			foreach ( var to in exchanges )
			{
				if ( from == to || all[from][to].Count == 0 )
					continue;

				var (best, leastWorst, profit) = GetBestPair ( all, from, to );
				if ( profit > bestGain )
				{
					bestGain = profit;
					result   = ( from, to, best, leastWorst, profit );
				}
			}

			return result;
		}
	}
}