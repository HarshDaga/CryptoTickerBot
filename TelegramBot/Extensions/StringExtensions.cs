using System;
using CryptoTickerBot.Data.Enums;

namespace TelegramBot.Extensions
{
	public static class StringExtensions
	{
		public static CryptoCoinId ToCryptoCoinId ( this string str, bool ignoreCase = true ) =>
			(CryptoCoinId) Enum.Parse ( typeof ( CryptoCoinId ), str, ignoreCase );
	}
}