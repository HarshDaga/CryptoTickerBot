using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Telegram.Extensions;
using Humanizer;
using Humanizer.Localisation;
using MoreLinq;
using Telegram.Bot.Types;

namespace CryptoTickerBot.Telegram.Keyboard
{
	internal class ExchangeInfoListMenu : TelegramKeyboardMenu
	{
		public ExchangeInfoListMenu ( TelegramBot telegramBot,
		                              User user,
		                              Chat chat,
		                              TelegramKeyboardMenu parent = null ) :
			base ( telegramBot, user, chat, "Select an exchange", parent: parent )
		{
			Labels = Enumerable.Append (
					Ctb.Exchanges.Keys
						.Select ( x => x.ToString ( ) )
						.Batch ( 2 ),
					new[] {"back"}
				)
				.ToList ( );

			BuildKeyboard ( );
			AddHandlers ( );
		}

		private void AddHandlers ( )
		{
			foreach ( var label in Labels.SelectMany ( x => x ) )
				Handlers[label] = ExchangeHandler;

			Handlers["back"] = BackHandler;
		}

		private async Task<TelegramKeyboardMenu> ExchangeHandler ( CallbackQuery query )
		{
			Ctb.TryGetExchange ( query.Data, out var exchange );

			await Client.SendTextBlockAsync ( Chat, GetSummary ( exchange ), cancellationToken: CancellationToken );

			return Parent;
		}

		private string GetSummary ( ICryptoExchange exchange )
		{
			var sb = new StringBuilder ( );

			sb.AppendLine ( $"Name: {exchange.Name}" );
			sb.AppendLine ( $"Is Running: {exchange.IsStarted}" );
			sb.AppendLine ( $"Url: {exchange.Url}" );
			sb.AppendLine ( $"Up Time: {exchange.UpTime.Humanize ( 2, minUnit: TimeUnit.Second )}" );
			sb.AppendLine ( $"Last Change: {exchange.LastChangeDuration.Humanize ( 2 )}" );
			sb.AppendLine ( $"Base Symbols: {exchange.BaseSymbols.Humanize ( )}" );
			sb.AppendLine ( $"Total Pairs: {exchange.Count}" );

			return sb.ToString ( );
		}
	}
}