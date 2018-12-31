using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoinbasePro;
using CoinbasePro.Shared.Types;
using CoinbasePro.WebSocket.Models.Response;
using CoinbasePro.WebSocket.Types;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Data.Domain;
using EnumsNET;
using NLog;
using WebSocket4Net;

namespace CryptoTickerBot.Core.Exchanges
{
	public class CoinbaseExchange : CryptoExchangeBase<Ticker>
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public CoinbaseProClient Client { get; private set; }

		public CoinbaseExchange ( ) : base ( CryptoExchangeId.Coinbase )
		{
		}

		protected override async Task GetExchangeDataAsync ( CancellationToken ct )
		{
			var closed = false;
			Client = new CoinbaseProClient ( );

			Client.WebSocket.OnWebSocketError += ( sender,
			                                       args ) =>
			{
			};
			Client.WebSocket.OnErrorReceived += ( sender,
			                                      args ) =>
			{
				Logger.Error ( args.LastOrder.Reason );
				closed = true;
			};

			StartWebSocket ( );

			while ( Client.WebSocket.State != WebSocketState.Closed )
			{
				if ( UpTime > LastUpdateDuration &&
				     LastUpdateDuration > TimeSpan.FromHours ( 1 ) ||
				     closed )
				{
					Client.WebSocket.Stop ( );
					break;
				}

				await Task.Delay ( PollingRate, ct ).ConfigureAwait ( false );
			}
		}

		private void StartWebSocket ( )
		{
			var products = Enums.GetValues<ProductType> ( ).Except ( new[] {ProductType.Unknown} ).ToList ( );
			var channels = new List<ChannelType> {ChannelType.Ticker};

			Client.WebSocket.OnTickerReceived += ( sender,
			                                       args ) =>
			{
				var ticker = args.LastOrder;
				Update ( ticker, ticker.ProductId );
			};
			Client.WebSocket.Start ( products, channels );
		}

		protected override void DeserializeData ( Ticker data,
		                                          string id )
		{
			ExchangeData[id].LowestAsk  = data.BestAsk;
			ExchangeData[id].HighestBid = data.BestBid;
			ExchangeData[id].Rate       = data.Price;
		}

		public override Task StopReceivingAsync ( )
		{
			if ( Client.WebSocket.State != WebSocketState.Closed )
				Client.WebSocket.Stop ( );

			return base.StopReceivingAsync ( );
		}
	}
}