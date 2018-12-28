using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using CryptoTickerBot.Arbitrage.IntraExchange;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Extensions;

namespace CryptoTickerBot.Core
{
	public class Markets
	{
		public CoreConfig.CryptoExchangeApiInfo Exchange { get; }
		public ImmutableHashSet<string> BaseSymbols { get; }
		public ImmutableDictionary<string, IDictionary<string, CryptoCoin>> Data { get; }

		public IReadOnlyDictionary<string, CryptoCoin> this [ string baseSymbol ] =>
			(IReadOnlyDictionary<string, CryptoCoin>) ( Data.TryGetValue ( baseSymbol, out var dict )
				? dict
				: null );

		public Graph Graph { get; }

		public CryptoCoin this [ string baseSymbol,
		                         string symbol ]
		{
			get
			{
				if ( !Data.TryGetValue ( baseSymbol, out var dict ) )
					return null;
				return dict.TryGetValue ( symbol, out var coin ) ? coin : null;
			}
			private set
			{
				if ( !Data.TryGetValue ( baseSymbol, out var dict ) )
					return;
				dict[symbol] = value;
			}
		}

		public Markets ( CoreConfig.CryptoExchangeApiInfo exchange )
		{
			Exchange    = exchange;
			BaseSymbols = ImmutableHashSet<string>.Empty;
			Data        = ImmutableDictionary<string, IDictionary<string, CryptoCoin>>.Empty;

			foreach ( var baseSymbol in exchange.BaseSymbols )
			{
				BaseSymbols = BaseSymbols.Add ( baseSymbol );
				Data        = Data.Add ( baseSymbol, new ConcurrentDictionary<string, CryptoCoin> ( ) );
			}

			Graph = new Graph ( exchange.Id );
		}

		private decimal GetAdjustedSellPrice ( CryptoCoin coin ) =>
			coin.SellPrice * ( 1m - Exchange.SellFees / 100m );

		private decimal GetAdjustedBuyPrice ( CryptoCoin coin ) =>
			coin.BuyPrice * ( 1m + Exchange.BuyFees / 100m );

		public bool AddOrUpdate ( CryptoCoin coin )
		{
			if ( coin.Spread < 0 )
				return false;

			foreach ( var baseSymbol in BaseSymbols )
			{
				if ( !coin.Symbol.EndsWith ( baseSymbol, StringComparison.OrdinalIgnoreCase ) )
					continue;

				var symbol = coin.Symbol.ReplaceLastOccurrence ( baseSymbol, "" );
				this[baseSymbol, symbol] = coin;

				Graph.UpsertEdge ( symbol, baseSymbol,
				                   GetAdjustedSellPrice ( coin ) );
				Graph.UpsertEdge ( baseSymbol, symbol,
				                   1m / GetAdjustedBuyPrice ( coin ) );

				return true;
			}

			return false;
		}
	}
}