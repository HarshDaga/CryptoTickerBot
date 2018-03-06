using System;
using System.Diagnostics;
using CryptoTickerBot.Data.Enums;

namespace TelegramBot.Extensions
{
	public static class StringExtensions
	{
		[DebuggerStepThrough]
		public static CryptoCoinId ToCryptoCoinId ( this string str, bool ignoreCase = true ) =>
			(CryptoCoinId) Enum.Parse ( typeof ( CryptoCoinId ), str, ignoreCase );
	}
}