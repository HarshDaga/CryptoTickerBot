using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Helpers;
using PusherClient;

// ReSharper disable StringIndexOfIsCultureSpecific.1

namespace CryptoTickerBot.Exchanges
{
	public class KoinexExchange : CryptoExchangeBase
	{
		private static readonly Dictionary<string, string> ToSymBol = new Dictionary<string, string>
		{
			["bitcoin"] = "BTC",
			["bitcoin_cash"] = "BCH",
			["ether"] = "ETH",
			["litecoin"] = "LTC",
		};

		private const string ApplicationKey = "9197b0bfdf3f71a4064e";

		public KoinexExchange ( )
		{
			Name = "Koinex";
			Url = "https://koinex.in/";
			TickerUrl = "wss://ws-ap2.pusher.com/app/9197b0bfdf3f71a4064e?protocol=7&client=js&version=4.1.0&flash=false";
			Id = CryptoExchange.Koinex;
		}

		public override async Task GetExchangeData ( CancellationToken ct )
		{
			ExchangeData = new Dictionary<string, CryptoCoin> ( );

			var pusher = new Pusher ( ApplicationKey, new PusherOptions { Cluster = "ap2" } );
			pusher.Error += ( sender, error ) => Console.WriteLine ( error );
			pusher.Connect ( );
			foreach ( var name in ToSymBol.Keys )
			{
				var channel = pusher.Subscribe ( $"my-channel-{name}" );
				channel.BindAll ( Listener );
			}

			await Task.Delay ( int.MaxValue, ct );
		}

		private void Listener ( string eventName, dynamic data )
		{
			if ( !eventName.EndsWith ( "_market_data" ) )
				return;

			var prefix = eventName.Substring ( 0, eventName.IndexOf ( "_market_data" ) );
			if ( !ToSymBol.ContainsKey ( prefix ) )
				return;
			var symbol = ToSymBol[prefix];

			Update ( data.message.data, symbol );
		}

		protected override void Update ( dynamic data, string symbol )
		{
			if ( !ExchangeData.ContainsKey ( symbol ) )
				ExchangeData[symbol] = new CryptoCoin ( symbol );

			var old = ExchangeData[symbol].Clone ( );

			decimal InrToUsd ( decimal amount ) => FiatConverter.Convert ( amount, FiatCurrency.INR, FiatCurrency.USD );

			ExchangeData[symbol].LowestAsk = InrToUsd ( data.lowest_ask );
			ExchangeData[symbol].HighestBid = InrToUsd ( data.highest_bid );
			ExchangeData[symbol].Rate = InrToUsd ( data.last_traded_price );

			if ( old != ExchangeData[symbol] )
				OnChanged ( this, old );
		}
	}
}