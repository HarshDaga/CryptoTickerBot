// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Quobject.SocketIoClientDotNet.Client;
using static System.Console;

#pragma warning disable 4014

namespace CryptoTickerBot
{
	public enum CryptoCompareExchanges
	{
		Cryptsy,
		BTCChina,
		Bitstamp,
		OKCoin,
		Coinbase,
		Poloniex,
		Cexio,
		BTCE,
		BitTrex,
		Kraken,
		Bitfinex,
		LocalBitcoins,
		itBit,
		HitBTC,
		Coinfloor,
		Huobi,
		LakeBTC,
		Coinsetter,
		CCEX,
		MonetaGo,
		Gatecoin,
		Gemini,
		CCEDK,
		Exmo,
		Yobit,
		BitBay,
		QuadrigaCX,
		BitSquare,
		TheRockTrading,
		bitFlyer,
		Quoine,
		LiveCoin,
		WavesDEX,
		Lykke,
		Remitano,
		Coinroom,
		Abucoins,
		TrustDEX
	}

	public class CryptoCompare
	{
		#region Constants

		public const string StreamUrl = "https://streamer.cryptocompare.com/";

		public const string BTC = "BTC";
		public const string LTC = "LTC";
		public const string ETH = "ETH";
		public const string BCH = "BCH";
		public const string USD = "USD";
		public const string EUR = "EUR";
		public const string GBP = "GBP";

		private const NumberStyles NumberStyle = NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint;

		#endregion

		#region Response Structs

		public enum SubscriptionId
		{
			Trade = 0,
			Current = 2,
			CurrentAgg = 5
		}

		public enum TradeFlag
		{
			Buy = 1,
			Sell = 2,
			Unknown = 4
		}

		public enum CurrentFlag
		{
			PriceUp = 1,
			PriceDown = 2,
			PriceUnchanged = 4
		}

		public class TradeResponse
		{
			public string ExchangeName { get; private set; }
			public string Crypto { get; private set; }
			public string Currency { get; private set; }
			public TradeFlag Flag { get; private set; }
			public decimal TradeId { get; private set; }
			public DateTime TimeStamp { get; private set; }
			public decimal Quantity { get; private set; }
			public decimal Price { get; private set; }
			public decimal Total { get; private set; }

			public TradeResponse ParseThis ( IReadOnlyList<string> response )
			{
				ExchangeName = response[1];
				Crypto = response[2];
				Currency = response[3];
				Flag = (TradeFlag) int.Parse ( response[4] );
				TradeId = decimal.Parse ( response[5], NumberStyle );
				TimeStamp = DateTimeOffset.FromUnixTimeSeconds ( long.Parse ( response[6] ) ).UtcDateTime;
				Quantity = decimal.Parse ( response[7], NumberStyle );
				Price = decimal.Parse ( response[8], NumberStyle );
				Total = decimal.Parse ( response[9], NumberStyle );

				return this;
			}

			public static TradeResponse Parse ( IReadOnlyList<string> response ) =>
				new TradeResponse ( ).ParseThis ( response );

			public override string ToString ( ) =>
				$"[Trade]   {ExchangeName,-15} {Crypto} {Flag,-10} {Price:C}";
		}

		public class CurrentResponse
		{
			public string ExchangeName { get; private set; }
			public string Crypto { get; private set; }
			public string Currency { get; private set; }
			public CurrentFlag Flag { get; private set; }
			public decimal Price { get; private set; }
			public DateTime LastUpdate { get; private set; }
			public decimal LastVolume { get; private set; }
			public decimal LastVolumeTo { get; private set; }
			public decimal LastTradeId { get; private set; }
			public decimal Volume24h { get; private set; }
			public decimal Volume24hTo { get; private set; }
			public decimal Average24h { get; private set; }
			public decimal Low24h { get; private set; }
			public decimal High24h { get; private set; }

			public CurrentResponse ParseThis ( IReadOnlyList<string> response )
			{
				try
				{
					ExchangeName = response[1];
					Crypto = response[2];
					Currency = response[3];
					Flag = (CurrentFlag) int.Parse ( response[4] );

					if ( response.Count <= 14 )
						ParseUpdate ( response );
					else
						ParseFirst ( response );
				}
				catch ( Exception e )
				{
					WriteLine ( $"{this}\n{e}" );
				}

				return this;
			}

			private void ParseFirst ( IReadOnlyList<string> response )
			{
				Price = decimal.Parse ( response[5], NumberStyle );
				LastUpdate = DateTimeOffset.FromUnixTimeSeconds ( long.Parse ( response[6] ) ).UtcDateTime;
				LastVolume = decimal.Parse ( response[7], NumberStyle );
				LastVolumeTo = decimal.Parse ( response[8], NumberStyle );
				LastTradeId = decimal.Parse ( response[9], NumberStyle );
				Volume24h = decimal.Parse ( response[10], NumberStyle );
				Volume24hTo = decimal.Parse ( response[11], NumberStyle );
				Average24h = decimal.Parse ( response[12], NumberStyle );
				High24h = decimal.Parse ( response[13], NumberStyle );
				Low24h = decimal.Parse ( response[14], NumberStyle );
			}

			private void ParseUpdate ( IReadOnlyList<string> response )
			{
				var i = 4;
				if ( Flag == CurrentFlag.PriceUnchanged )
					--i;
				Price = decimal.Parse ( response[++i], NumberStyle );
				LastUpdate = DateTimeOffset.FromUnixTimeSeconds ( long.Parse ( response[++i] ) ).UtcDateTime;
				LastVolume = decimal.Parse ( response[++i], NumberStyle );
				LastVolumeTo = decimal.Parse ( response[++i], NumberStyle );
				LastTradeId = int.Parse ( response[++i], NumberStyle );
				Volume24h = decimal.Parse ( response[++i], NumberStyle );
				Volume24hTo = decimal.Parse ( response[++i], NumberStyle );
			}

			public static CurrentResponse Parse ( IReadOnlyList<string> response ) =>
				new CurrentResponse ( ).ParseThis ( response );

			public override string ToString ( ) =>
				$"[Current] {ExchangeName,-15} {Crypto} {Flag,-16} {Price:C}";
		}

		#endregion

		public HashSet<CryptoCompareExchanges> TradeExchanges = new HashSet<CryptoCompareExchanges> ( );
		public HashSet<CryptoCompareExchanges> CurrentExchanges = new HashSet<CryptoCompareExchanges> ( );
		public HashSet<string> Cryptos = new HashSet<string> {BTC, LTC, ETH, BCH};
		public HashSet<string> Currencies = new HashSet<string> {USD};

		public event Action<TradeResponse> Trade;
		public event Action<CurrentResponse> Current;

		public void StartMonitor ( )
		{
			Task.Run ( async ( ) =>
			{
				try
				{
					var socket = IO.Socket ( StreamUrl, new IO.Options {AutoConnect = false} );
					socket.On ( Socket.EVENT_ERROR, OnError );
					var tradeDict = CreateDictionary ( SubscriptionId.Trade, TradeExchanges );
					var currentDict = CreateDictionary ( SubscriptionId.Current, CurrentExchanges );
					socket.On ( Socket.EVENT_CONNECT, ( ) =>
					{
						WriteLine ( "Connected" );
						if ( tradeDict["subs"].Count != 0 )
							socket.Emit ( "SubAdd", ToJson ( tradeDict ) );
						if ( currentDict["subs"].Count != 0 )
							socket.Emit ( "SubAdd", ToJson ( currentDict ) );
					} );
					socket.On ( "m", OnMessage );
					socket.Connect ( );

					await Task.Delay ( int.MaxValue );
				}
				catch ( Exception e )
				{
					WriteLine ( e );
				}
			} );
		}

		private static object ToJson ( object tradeDict )
		{
			var json = JsonConvert.SerializeObject ( tradeDict );
			return JsonConvert.DeserializeObject ( json );
		}

		private Dictionary<string, List<object>> CreateDictionary ( SubscriptionId type,
			IEnumerable<CryptoCompareExchanges> exchanges )
		{
			var tradeDict = new Dictionary<string, List<object>> {["subs"] = new List<object> ( )};
			foreach ( var exchange in exchanges )
			{
				foreach ( var crypto in Cryptos )
				foreach ( var currency in Currencies )
					tradeDict["subs"].Add ( $"{(int) type}~{exchange}~{crypto}~{currency}" );
			}

			return tradeDict;
		}

		#region Event Handlers

		private void OnMessage ( object ob )
		{
			var response = ( (string) ob ).Split ( '~' );
			var subscriptionId = (SubscriptionId) int.Parse ( response[0] );
			if ( subscriptionId == SubscriptionId.Trade )
				HandleTrade ( response );
			else if ( subscriptionId == SubscriptionId.Current )
				HandleCurrent ( response );
		}

		private void HandleCurrent ( IReadOnlyList<string> response )
		{
			var current = CurrentResponse.Parse ( response );
			Current?.Invoke ( current );
		}

		private void HandleTrade ( IReadOnlyList<string> response )
		{
			var trade = TradeResponse.Parse ( response );
			Trade?.Invoke ( trade );
		}

		private static void OnError ( object ob ) => WriteLine ( $"Error: {ob}" );

		#endregion
	}
}