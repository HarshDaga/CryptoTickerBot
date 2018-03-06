using System.Diagnostics;

namespace TelegramBot.Extensions
{
	public static class DecimalExtensions
	{
		[DebuggerStepThrough]
		public static string ToCurrency ( this decimal d ) =>
			d < 0 ? $"-{-d:C}" : $"{d:C}";
	}
}