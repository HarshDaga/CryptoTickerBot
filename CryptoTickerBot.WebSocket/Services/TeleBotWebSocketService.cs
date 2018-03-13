using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.WebSocket.Messages;
using TelegramBot;
using TelegramBot.Core;

namespace CryptoTickerBot.WebSocket.Services
{
	public class TeleBotWebSocketService : CtbWebSocketService
	{
		protected TeleBot Bot { get; }

		public TeleBotWebSocketService ( TeleBot bot ) : base ( bot.Ctb )
		{
			Bot = bot;

			AvailableSubscriptions["TeleSubscriptionUpdates"] = SubscribeToTeleSubscriptionUpdates;

			AvailableCommands["GetExchangeStatuses"]     = GetExchangeStatusesCommand;
			AvailableCommands["GetTeleSubscriptionList"] = GetTeleSubscriptionListCommand;
			AvailableCommands["GetTeleSubscription"]     = GetTeleSubscriptionCommand;
		}

		#region SubscriptionHandlers

		private void SubscribeToTeleSubscriptionUpdates ( string s, WebSocketIncomingMessage im )
		{
			if ( !( im.Data is long id ) ) return;
			var sub = Bot.Subscriptions.FirstOrDefault ( x => x.Id == id );

			if ( sub is null )
			{
				Send ( WebSocketMessageBuilder.Error ( $"{id} not found is subscriptions." ) );
				return;
			}

			Task SubOnUpdated ( TelegramSubscription _, CryptoCoin __, CryptoCoin ___ ) =>
				Task.Run ( ( ) => Send ( s, new TeleBotSubscriptionSummary ( Bot, (int) id ) ) );

			sub.Updated += SubOnUpdated;
		}

		#endregion

		#region CommandHandlers

		private void GetExchangeStatusesCommand ( string s, WebSocketIncomingMessage im )
		{
			var list = Bot.Exchanges.Values.Select ( x => new
				{
					x.Name,
					x.UpTime,
					x.LastUpdate,
					x.LastChange
				} )
				.ToList ( );

			Send ( s, list );
		}

		private void GetTeleSubscriptionListCommand ( string s, WebSocketIncomingMessage im )
		{
			var list = Bot.Subscriptions
				.Select ( x => new TeleBotSubscriptionSummary ( Bot, x.Id ) )
				.ToList ( );

			Send ( s, list );
		}

		private void GetTeleSubscriptionCommand ( string s, WebSocketIncomingMessage im )
		{
			Send (
				s,
				im.Data is long id ? new TeleBotSubscriptionSummary ( Bot, (int) id ) : null
			);
		}

		#endregion
	}
}