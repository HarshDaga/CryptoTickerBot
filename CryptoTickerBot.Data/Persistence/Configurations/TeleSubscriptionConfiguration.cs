using System.Data.Entity.ModelConfiguration;
using CryptoTickerBot.Data.Domain;

namespace CryptoTickerBot.Data.Persistence.Configurations
{
	internal class TeleSubscriptionConfiguration : EntityTypeConfiguration<TeleSubscription>
	{
		public TeleSubscriptionConfiguration ( )
		{
			Property ( s => s.Threshold )
				.HasPrecision ( 18, 4 );

			HasMany ( s => s.Coins )
				.WithMany ( )
				.Map ( m => m
					       .ToTable ( "TeleSubscriptionCoins" )
					       .MapLeftKey ( "SubscriptionId" )
					       .MapRightKey ( "CoinId" )
				);
		}
	}
}