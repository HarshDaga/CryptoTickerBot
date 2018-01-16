using System;
using System.Threading;
using CryptoTickerBot.Exchanges;
using CryptoTickerBot.Helpers;

// ReSharper disable FunctionNeverReturns
#pragma warning disable 4014

namespace CryptoTickerBot
{
	public class Program
	{
		public static void Main ( string[] args )
		{
			FiatConverter.StartMonitor ( );

			var koinex = new KoinexExchange ( );
			var bitbay = new BitBayExchange ( );
			var binance = new BinanceExchange ( );
			koinex.OnChanged += Exchange_OnChanged;
			bitbay.OnChanged += Exchange_OnChanged;
			binance.OnChanged += Exchange_OnChanged;
			koinex.GetExchangeData ( CancellationToken.None );
			bitbay.GetExchangeData ( CancellationToken.None );
			binance.GetExchangeData ( CancellationToken.None );
			while ( true )
			{
				Thread.Sleep ( 1 );
			}
		}

		private static void Exchange_OnChanged ( ICryptoExchange cryptoExchange, CryptoCoin coin ) =>
			Console.WriteLine ( $"{cryptoExchange.Name,-10} {cryptoExchange.ExchangeData[coin.Symbol]}" );
	}
}