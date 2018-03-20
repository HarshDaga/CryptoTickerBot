using System.Data.Entity.ModelConfiguration;
using CryptoTickerBot.Data.Domain;

namespace CryptoTickerBot.Data.Persistence.Configurations
{
	internal class CryptoExchangeConfiguration : EntityTypeConfiguration<CryptoExchange>
	{
		public CryptoExchangeConfiguration ( )
		{
			HasMany ( e => e.CoinValues )
				.WithRequired ( ccv => ccv.Exchange )
				.HasForeignKey ( ccv => ccv.ExchangeId )
				.WillCascadeOnDelete ( );

			HasMany ( e => e.WithdrawalFees )
				.WithRequired ( f => f.Exchange )
				.HasForeignKey ( f => f.ExchangeId )
				.WillCascadeOnDelete ( );

			HasMany ( e => e.DepositFees )
				.WithRequired ( f => f.Exchange )
				.HasForeignKey ( f => f.ExchangeId )
				.WillCascadeOnDelete ( );

			HasMany ( e => e.LatestCoinValues )
				.WithMany ( )
				.Map ( configuration =>
					       configuration.ToTable ( "LatestCoinValues" )
						       .MapLeftKey ( "ExchangeId" )
						       .MapRightKey ( "CoinValueId" )
				);
		}
	}
}