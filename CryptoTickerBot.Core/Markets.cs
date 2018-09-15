using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Extensions;

namespace CryptoTickerBot.Core
{
	public class Markets
	{
		public ImmutableHashSet<string> BaseSymbols { get; }
		public ImmutableDictionary<string, IDictionary<string, CryptoCoin>> Data { get; }

		public IReadOnlyDictionary<string, CryptoCoin> this [ string baseSymbol ] =>
			(IReadOnlyDictionary<string, CryptoCoin>) ( Data.TryGetValue ( baseSymbol, out var dict )
				? dict
				: null );

		public CryptoCoin this [ string baseSymbol,
		                         string symbol ]
		{
			get
			{
				if ( !Data.TryGetValue ( baseSymbol, out var dict ) )
					return null;
				return dict.TryGetValue ( symbol, out var coin ) ? coin : null;
			}
			set
			{
				if ( !Data.TryGetValue ( baseSymbol, out var dict ) )
					return;
				dict[symbol] = value;
			}
		}

		public Markets ( IEnumerable<string> baseSymbols )
		{
			BaseSymbols = ImmutableHashSet<string>.Empty;
			Data        = ImmutableDictionary<string, IDictionary<string, CryptoCoin>>.Empty;

			foreach ( var baseSymbol in baseSymbols )
			{
				BaseSymbols = BaseSymbols.Add ( baseSymbol );
				Data        = Data.Add ( baseSymbol, new ConcurrentDictionary<string, CryptoCoin> ( ) );
			}
		}

		public bool AddOrUpdate ( CryptoCoin coin )
		{
			foreach ( var baseSymbol in BaseSymbols )
			{
				if ( !coin.Symbol.EndsWith ( baseSymbol, StringComparison.OrdinalIgnoreCase ) )
					continue;

				var symbol = coin.Symbol.ReplaceLastOccurrence ( baseSymbol, "" );
				this[baseSymbol, symbol] = coin;

				return true;
			}

			return false;
		}
	}
}