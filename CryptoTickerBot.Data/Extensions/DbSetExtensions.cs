using System.Data.Entity;

namespace CryptoTickerBot.Data.Extensions
{
	public static class DbSetExtensions
	{
		public static T AddIfNotExists<T> ( this IDbSet<T> set, T value, params object[] keyValues )
			where T : class =>
			set.Find ( keyValues ) == null ? set.Add ( value ) : value;
	}
}