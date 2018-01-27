namespace TelegramBot.Extensions
{
	public static class DecimalExtensions
	{
		public static string ToCurrency ( this decimal d ) =>
			d < 0 ? $"-{-d:C}" : $"{d:C}";
	}
}