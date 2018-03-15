using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Exchanges.Core;
using CryptoTickerBot.WebSocket.Extensions;
using CryptoTickerBot.WebSocket.Messages;
using NLog;
using WebSocketSharp;
using WebSocketSharp.Server;
using Logger = NLog.Logger;

namespace CryptoTickerBot.WebSocket.Services
{
	public class CtbWebSocketService : WebSocketBehavior
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		protected readonly Dictionary<string, Action<string, WebSocketIncomingMessage>>
			AvailableCommands;

		protected readonly Dictionary<string, Action<string, WebSocketIncomingMessage, bool>>
			AvailableSubscriptions;

		protected CryptoTickerBotCore Ctb { get; }
		protected HashSet<WebSocketIncomingMessage> Subscriptions { get; }

		public CtbWebSocketService ( CryptoTickerBotCore ctb )
		{
			Ctb           = ctb;
			Subscriptions = new HashSet<WebSocketIncomingMessage> ( );

			AvailableCommands = new Dictionary<string, Action<string, WebSocketIncomingMessage>>
			{
				["GetBestPair"] = GetBestPairCommand
			};
			AvailableSubscriptions = new Dictionary<string, Action<string, WebSocketIncomingMessage, bool>>
			{
				["CoinValueUpdates"] = SubscribeToCoinValueUpdates
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
			try
			{
				if ( args.IsText )
				{
					var message = args.Data;
					if ( message.TryDeserialize ( out WebSocketIncomingMessage im ) )
						switch ( im.Type )
						{
							case WssMessageType.Subscribe:
								HandleSubscription ( im );
								break;
							case WssMessageType.Command:
								HandleCommand ( im );
								break;
							case WssMessageType.Unsubscribe:
								HandleUnsubscribe ( im );
								break;
						}
				}
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
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
				kp.Value?.Invoke ( kp.Key, im, true );
			}
			else
			{
				Send ( WebSocketMessageBuilder.Error ( $"{im.Name} doesn't exist or already subscribed." ) );
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

		protected virtual void HandleUnsubscribe ( WebSocketIncomingMessage im )
		{
			if ( AvailableSubscriptions.Keys.Any ( x => x == im ) &&
			     Subscriptions.Remove ( im ) )
			{
				var kp = AvailableSubscriptions.First ( x => x.Key == im );
				kp.Value?.Invoke ( kp.Key, im, false );
				Send ( WebSocketMessageBuilder.Unsubscribe ( kp.Key ) );
			}
			else
			{
				Send ( WebSocketMessageBuilder.Error ( $"{im.Name} doesn't exist or not subscribed." ) );
			}
		}

		protected void Send ( string @event, dynamic data )
		{
			SendAsync ( new WebSocketMessage ( @event, data ), null );
		}

		#region CommandHandlers

		private void GetBestPairCommand ( string s, WebSocketIncomingMessage im )
		{
			Send ( s, new BestPairSummary ( Ctb.CompareTable ) );
		}

		#endregion

		#region SubscriptionHandlers

		private void SubscribeToCoinValueUpdates (
			string @event,
			WebSocketIncomingMessage _,
			bool subscribing
		)
		{
			foreach ( var exchange in Ctb.Exchanges.Values )
				if ( subscribing )
					exchange.Next += CoinValueOnNextHandler;
				else
					exchange.Next -= CoinValueOnNextHandler;
		}

		private void CoinValueOnNextHandler ( CryptoExchangeBase e, CryptoCoin c )
		{
			Send ( "CoinValueUpdate", new CryptoCoinSummary ( e, c ) );
		}

		#endregion
	}
}