using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Telegram.Interfaces;
using Telegram.Bot.Types;

namespace CryptoTickerBot.Telegram.Menus
{
	internal class TelegramMenuManager
	{
		protected ConcurrentDictionary<User, ConcurrentDictionary<long, IMenu>> Menus { get; }

		public IMenu this [ User user,
		                    long chatId ]
		{
			get
			{
				if ( Menus.TryGetValue ( user, out var userMenus ) )
					if ( userMenus.TryGetValue ( chatId, out var menu ) )
						return menu;

				return null;
			}
		}

		public IMenu this [ User user,
		                    Chat chat ] =>
			this[user, chat.Id];

		public IMenu this [ CallbackQuery query ] =>
			this[query.From, query.Message.Chat.Id];

		public IList<IMenu> this [ long chatId ] =>
			Menus.Values.Where ( x => x.ContainsKey ( chatId ) ).SelectMany ( x => x.Values ).ToList ( );

		public IList<IMenu> this [ Chat chat ] => this[chat.Id];

		public TelegramMenuManager ( )
		{
			Menus = new ConcurrentDictionary<User, ConcurrentDictionary<long, IMenu>> ( );
		}

		public bool TryGetMenu ( User user,
		                         long chatId,
		                         out IMenu menu )
		{
			menu = null;
			return Menus.TryGetValue ( user, out var menus ) && menus.TryGetValue ( chatId, out menu );
		}

		public bool Remove ( User user,
		                     long chatId ) =>
			Menus.TryGetValue ( user, out var menus ) && menus.TryRemove ( chatId, out _ );

		public void AddOrUpdateMenu ( IMenu menu )
		{
			if ( Menus.TryGetValue ( menu.User, out var userMenus ) )
				userMenus[menu.Chat.Id] = menu;
			else
				Menus[menu.User] = new ConcurrentDictionary<long, IMenu>
				{
					[menu.Chat.Id] = menu
				};
		}
	}
}