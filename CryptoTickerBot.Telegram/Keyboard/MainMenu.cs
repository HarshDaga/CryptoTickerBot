using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTickerBot.Telegram.Extensions;
using Humanizer;
using Humanizer.Localisation;
using MoreLinq;
using Tababular;
using Telegram.Bot.Types;

#pragma warning disable 1998

namespace CryptoTickerBot.Telegram.Keyboard
{
	internal class MainMenu : TelegramKeyboardMenu
	{
		public MainMenu ( TelegramBot telegramBot,
		                  User user,
		                  Chat chat ) :
			base ( telegramBot, user, chat, "Main Menu" )
		{
			Labels = new[] {"status", "exchange info", "exit"}
				.Batch ( 2 )
				.ToList ( );

			BuildKeyboard ( );
			AddHandlers ( );
		}

		private void AddHandlers ( )
		{
			Handlers["status"]        = StatusHandler;
			Handlers["exchange info"] = ExchangeInfoHandler;
			Handlers["exit"]          = BackHandler;
		}

		private async Task<TelegramKeyboardMenu> StatusHandler ( CallbackQuery query )
		{
			var formatter = new TableFormatter ( );
			var objects = Ctb.Exchanges.Values.Select (
					exchange => new Dictionary<string, string>
					{
						["Exchange"]    = exchange.Name,
						["Up Time"]     = exchange.UpTime.Humanize ( 2, minUnit: TimeUnit.Second ),
						["Last Update"] = exchange.LastUpdateDuration.Humanize ( )
					}
				)
				.Cast<IDictionary<string, string>> ( )
				.ToList ( );

			var builder = new StringBuilder ( );
			builder
				.AppendLine (
					$"Running since {( DateTime.UtcNow - Ctb.StartTime ).Humanize ( 3, minUnit: TimeUnit.Second )}" )
				.AppendLine ( "" )
				.AppendLine ( formatter.FormatDictionaries ( objects ) );

			await Client.SendTextBlockAsync ( Chat,
			                                  builder.ToString ( ),
			                                  cancellationToken: CancellationToken )
				.ConfigureAwait ( false );

			return this;
		}

		private async Task<TelegramKeyboardMenu> ExchangeInfoHandler ( CallbackQuery query ) =>
			new ExchangeInfoListMenu ( TelegramBot, User, Chat, this );
	}
}