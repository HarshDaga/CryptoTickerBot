using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;

namespace CryptoTickerBot.Telegram.Extensions
{
	public static class MessageExtensions
	{
		public static void ExtractCommand ( this Message message,
		                                    User self,
		                                    out string command,
		                                    out List<string> @params )
		{
			var text = message.Text;
			command = text.Split ( ' ' ).First ( );

			var index = command.IndexOf ( $"@{self.Username}", StringComparison.OrdinalIgnoreCase );
			if ( index != -1 )
				command = command.Substring ( 0, index );

			@params = text
				.Split ( new[] {' '}, StringSplitOptions.RemoveEmptyEntries )
				.Skip ( 1 )
				.ToList ( );
		}
	}
}