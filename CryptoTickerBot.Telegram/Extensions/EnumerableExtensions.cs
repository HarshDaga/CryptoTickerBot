using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using Telegram.Bot.Types.ReplyMarkups;

namespace CryptoTickerBot.Telegram.Extensions
{
	public static class EnumerableExtensions
	{
		public static InlineKeyboardMarkup ToInlineKeyboardMarkup ( this IEnumerable<string> labels ) =>
			new InlineKeyboardMarkup ( labels.Select ( x => x.ToKeyboardButton ( ) ) );

		public static InlineKeyboardMarkup ToInlineKeyboardMarkup ( this IEnumerable<string> labels,
		                                                            int batchSize ) =>
			new InlineKeyboardMarkup ( labels.Select ( x => x.ToKeyboardButton ( ) ).Batch ( batchSize ) );

		public static InlineKeyboardMarkup ToInlineKeyboardMarkup ( this IEnumerable<IEnumerable<string>> labels ) =>
			new InlineKeyboardMarkup ( labels.Select ( x => x.Select ( y => y.ToKeyboardButton ( ) ) ).ToArray ( ) );
	}
}