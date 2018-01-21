using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Extensions;

namespace CryptoTickerBot.Exchanges
{
	public class CryptoExchangeObserver : IObserver<CryptoCoin>
	{
		private readonly Dictionary<string, List<CryptoCoin>> history;
		private readonly Dictionary<string, List<PriceChange>> priceChanges;
		public CryptoExchangeBase Exchange { get; }

		public CryptoExchangeObserver ( CryptoExchangeBase exchange )
		{
			Exchange = exchange;
			history = new Dictionary<string, List<CryptoCoin>> ( );
			priceChanges = new Dictionary<string, List<PriceChange>> ( );
		}

		public void OnNext ( CryptoCoin coin )
		{
			if ( !history.ContainsKey ( coin.Symbol ) )
			{
				history[coin.Symbol] = new List<CryptoCoin> ( );
				priceChanges[coin.Symbol] = new List<PriceChange> ( );
			}

			if ( history[coin.Symbol].Any ( ) )
				priceChanges[coin.Symbol].Add ( coin - history[coin.Symbol].Last ( ) );
			history[coin.Symbol].Add ( coin );

			Next?.Invoke ( Exchange, coin );
		}

		public void OnError ( Exception error )
		{
		}

		public void OnCompleted ( )
		{
		}

		public event Action<CryptoExchangeBase, CryptoCoin> Next;

		public IList<CryptoCoin> History ( string symbol, TimeSpan timeSpan )
		{
			var start = DateTime.Now.Subtract ( timeSpan );
			return history[symbol].SkipWhile ( x => x.Time < start ).ToList ( );
		}

		public IList<CryptoCoin> History ( string symbol, int count ) =>
			history[symbol].TakeLast ( count );
	}
}