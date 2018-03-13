using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CryptoTickerBot.Exchanges.Core;
using CryptoTickerBot.WebSocket.Extensions;
using CryptoTickerBot.WebSocket.Messages;
using NLog;
using WebSocketSharp;
using WebSocketSharp.Server;
using CTB = CryptoTickerBot.Core.CryptoTickerBot;
using Logger = NLog.Logger;

namespace CryptoTickerBot.WebSocket.Services
{
	public class CtbWebSocketService : WebSocketBehavior
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		protected readonly Dictionary<string, Action<string, WebSocketIncomingMessage>>
			AvailableCommands;

		protected readonly Dictionary<string, Action<string, WebSocketIncomingMessage>>
			AvailableSubscriptions;

		protected CTB Ctb { get; }
		protected HashSet<WebSocketIncomingMessage> Subscriptions { get; }

		public CtbWebSocketService ( CTB ctb )
		{
			Ctb           = ctb;
			Subscriptions = new HashSet<WebSocketIncomingMessage> ( );

			AvailableSubscriptions = new Dictionary<string, Action<string, WebSocketIncomingMessage>>
			{
				["CoinValueUpdates"] = SubscribeToCoinValueUpdates,
				["BestPairUpdates"]  = SubscribeToBestPairUpdates
			};
			AvailableCommands = new Dictionary<string, Action<string, WebSocketIncomingMessage>>
			{
				["GetBestPair"] = GetBestPairCommand
			};
		}

		protected void SendUsage ( )
		{
			Send ( "Usage", new
			{
				Commands      = AvailableCommands.Keys,
				Subscriptions = AvailableSubscriptions.Keys,
				MessageFormat = WebSocketIncomingMessage.Format
			} );
		}

		protected override void OnOpen ( )
		{
			Logger.Info ( $"Client connected: {Context.Host}" );

			SendUsage ( );

			base.OnOpen ( );
		}

		protected override void OnClose ( CloseEventArgs e )
		{
			Logger.Info ( $"Connection closed: {Context.Host} {e.Reason}" );
			if ( !e.WasClean )
				Logger.Warn ( $"Unclean termination: {Context.Host} {e.Code}" );

			base.OnClose ( e );
		}

		protected override void OnMessage ( MessageEventArgs args )
		{
			if ( args.IsText )
			{
				var message = args.Data;
				if ( message.TryDeserialize ( out WebSocketIncomingMessage im ) )
					if ( im.Type == WssMessageType.Event )
						HandleSubscription ( im );
					else
						HandleCommand ( im );
			}

			base.OnMessage ( args );
		}

		protected virtual void HandleSubscription ( WebSocketIncomingMessage im )
		{
			if ( AvailableSubscriptions.Keys.Any ( x => x == im ) &&
			     Subscriptions.Add ( im ) )
			{
				var kp = AvailableSubscriptions.First ( x => x.Key == im );
				Send ( WebSocketMessageBuilder.Subscribe ( kp.Key ) );
				kp.Value?.Invoke ( kp.Key, im );
			}
			else
			{
				Send ( WebSocketMessageBuilder.Error ( $"{im.Name} Doesn't exist or already subscribed." ) );
			}
		}

		protected virtual void HandleCommand ( WebSocketIncomingMessage im )
		{
			if ( AvailableCommands.Keys.Any ( x => x == im ) )
			{
				var kp = AvailableCommands.First ( x => x.Key == im );
				kp.Value?.Invoke ( kp.Key, im );
			}
			else
			{
				Send ( WebSocketMessageBuilder.Error ( $"{im.Name} not a valid command." ) );
			}
		}

		#region CommandHandlers

		private void GetBestPairCommand ( string s, WebSocketIncomingMessage im )
		{
			Send ( s, new BestPairSummary ( Ctb.CompareTable ) );
		}

		#endregion

		#region SubscriptionHandlers

		private void SubscribeToCoinValueUpdates ( string @event, WebSocketIncomingMessage _ )
		{
			foreach ( var exchange in Ctb.Exchanges.Values )
				exchange.Next += CoinValueOnNextHandler;
		}

		private void CoinValueOnNextHandler ( CryptoExchangeBase e, CryptoCoin c )
		{
			Send ( "CoinValueUpdate", new CryptoCoinSummary ( e, c ) );
		}

		protected void Send ( string @event, dynamic data )
		{
			SendAsync ( new WebSocketMessage ( @event, data ), null );
		}

		private void SubscribeToBestPairUpdates ( string @event, WebSocketIncomingMessage _ )
		{
			Task.Run ( ( ) =>
				{
					var timer = new Timer ( 1000 );
					timer.Elapsed += ( sender, args ) =>
						Send ( @event, new BestPairSummary ( Ctb.CompareTable ) );
					timer.Start ( );
				}
			);
		}

		#endregion
	}
}