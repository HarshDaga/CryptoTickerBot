using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Exchanges;
using MoreLinq;

namespace CryptoTickerBot.Helpers
{
	public class CryptoCompareTable
	{
		public Dictionary<CryptoExchange, CryptoExchangeBase> Exchanges { get; set; } =
			new Dictionary<CryptoExchange, CryptoExchangeBase> ( );

		public CryptoCompareTable ( params CryptoExchangeBase[] exchanges )
		{
			foreach ( var exchange in exchanges )
				AddExchange ( exchange );
		}

		public void AddExchange ( CryptoExchangeBase exchange ) =>
			Exchanges[exchange.Id] = exchange;

		public Dictionary<string, decimal> GetPair ( CryptoExchange from, CryptoExchange to )
		{
			if ( !Exchanges.ContainsKey ( from ) || !Exchanges.ContainsKey ( to ) )
				return null;

			var result = new Dictionary<string, decimal> ( );
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

		public Dictionary<CryptoExchange, Dictionary<string, decimal>> GetAll ( CryptoExchange from )
		{
			if ( !Exchanges.ContainsKey ( from ) )
				return null;

			var result = new Dictionary<CryptoExchange, Dictionary<string, decimal>> ( );
			foreach ( var to in Exchanges.Keys )
				result[to] = GetPair ( from, to );

			return result;
		}

		public Dictionary<CryptoExchange, Dictionary<CryptoExchange, Dictionary<string, decimal>>> GetAll ( )
		{
			var result = new Dictionary<CryptoExchange, Dictionary<CryptoExchange, Dictionary<string, decimal>>> ( );
			foreach ( var exchange in Exchanges.Keys )
				result[exchange] = GetAll ( exchange );

			return result;
		}

		public Dictionary<CryptoExchange, Dictionary<CryptoExchange, Dictionary<string, decimal>>> Get (
			params CryptoExchange[] exchanges
		)
		{
			var result = new Dictionary<CryptoExchange, Dictionary<CryptoExchange, Dictionary<string, decimal>>> ( );
			foreach ( var exchange in exchanges.Intersect ( Exchanges.Keys ) )
				result[exchange] = GetAll ( exchange )
					.Where ( pair => exchanges.Contains ( pair.Key ) )
					.ToDictionary ( );

			return result;
		}

		public static void RemoveExchange (
			Dictionary<CryptoExchange, Dictionary<string, decimal>> compare,
			params CryptoExchange[] cryptoExchanges
		)
		{
			foreach ( var exchange in cryptoExchanges )
				compare.Remove ( exchange );
		}

		public static void RemoveExchange (
			Dictionary<CryptoExchange, Dictionary<CryptoExchange, Dictionary<string, decimal>>> compare,
			params CryptoExchange[] cryptoExchanges
		)
		{
			foreach ( var exchange in cryptoExchanges )
				compare.Remove ( exchange );

			foreach ( var compareValue in compare.Values )
				RemoveExchange ( compareValue, cryptoExchanges );
		}

		public static (string best, string leastWorst, decimal profit) GetBestPair (
			Dictionary<CryptoExchange, Dictionary<CryptoExchange, Dictionary<string, decimal>>> compare,
			CryptoExchange from,
			CryptoExchange to
		)
		{
			var best = compare[from][to].MaxBy ( x => x.Value ).Key;
			var leastWorst = compare[to][from].MaxBy ( x => x.Value ).Key;
			var profit =
				( 1m + compare[from][to][best] )
				* ( 1m + compare[to][from][leastWorst] )
				- 1m;

			return (best, leastWorst, profit);
		}

		public (string best, string leastWorst, decimal profit) GetBestPair ( CryptoExchange from, CryptoExchange to )
		{
			var compare = GetAll ( );
			return GetBestPair ( compare, from, to );
		}

		public (CryptoExchange from, CryptoExchange to, string first, string second, decimal profit) GetBest ( )
		{
			var all = GetAll ( );
			var exchanges = Exchanges.Keys.ToList ( );
			var bestGain = decimal.MinValue;
			(CryptoExchange from, CryptoExchange to, string first, string second, decimal profit) result = default;

			foreach ( var from in exchanges )
			foreach ( var to in exchanges )
			{
				if ( from == to || all[from][to].Count == 0 )
					continue;

				var (best, leastWorst, profit) = GetBestPair ( all, from, to );
				if ( profit > bestGain )
				{
					bestGain = profit;
					result   = (from, to, best, leastWorst, profit);
				}
			}

			return result;
		}
	}
}