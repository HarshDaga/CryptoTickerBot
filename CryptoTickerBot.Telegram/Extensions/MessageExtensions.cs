using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;

namespace CryptoTickerBot.Telegram.Extensions
{
	public static class MessageExtensions
	{
		public static (string command, List<string> @params ) ExtractCommand ( this Message message,
		                                                                       User self )
		{
			var text = message.Text;
			var command = text.Split ( ' ' ).First ( );

			var index = command.IndexOf ( $"@{self.Username}", StringComparison.OrdinalIgnoreCase );
			if ( index != -1 )
				command = command.Substring ( 0, index );

			var @params = text
				.Split ( new[] {' '}, StringSplitOptions.RemoveEmptyEntries )
				.Skip ( 1 )
				.ToList ( );

			return ( command, @params );
		}
	}
}