using System.Data.Entity;
using CryptoTickerBot.Data.Domain;
using NLog;

namespace CryptoTickerBot.Data.Persistence
{
	public class CtbContext : DbContext
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public DbSet<CryptoCoin> Coins { get; set; }
		public DbSet<CryptoCoinValue> CoinValues { get; set; }
		public DbSet<CryptoExchange> Exchanges { get; set; }
		public DbSet<WithdrawalFees> WithdrawalFees { get; set; }
		public DbSet<DepositFees> DepositFees { get; set; }
		public DbSet<TeleBotUser> TeleBotUsers { get; set; }
		public DbSet<TeleSubscription> TeleSubscriptions { get; set; }

		public CtbContext ( )
			: base ( "name=DefaultConnection" )
		{
			Database.Log = Logger.Trace;
			Database.SetInitializer ( new CtbContextInitialzer ( ) );
		}

		protected override void OnModelCreating ( DbModelBuilder modelBuilder )
		{
			modelBuilder.Entity<CryptoExchange> ( )
				.HasMany ( e => e.CoinValues )
				.WithRequired ( ccv => ccv.Exchange )
				.HasForeignKey ( ccv => ccv.ExchangeId )
				.WillCascadeOnDelete ( );

			modelBuilder.Entity<CryptoExchange> ( )
				.HasMany ( e => e.WithdrawalFees )
				.WithRequired ( f => f.Exchange )
				.HasForeignKey ( f => f.ExchangeId )
				.WillCascadeOnDelete ( );

			modelBuilder.Entity<CryptoExchange> ( )
				.HasMany ( e => e.DepositFees )
				.WithRequired ( f => f.Exchange )
				.HasForeignKey ( f => f.ExchangeId )
				.WillCascadeOnDelete ( );

			modelBuilder.Entity<WithdrawalFees> ( )
				.Property ( e => e.Value )
				.HasPrecision ( 18, 6 );

			modelBuilder.Entity<DepositFees> ( )
				.Property ( e => e.Value )
				.HasPrecision ( 18, 6 );

			modelBuilder.Entity<TeleSubscription> ( )
				.Property ( s => s.Threshold )
				.HasPrecision ( 18, 4 );

			modelBuilder.Entity<TeleSubscription> ( )
				.HasMany ( s => s.Coins )
				.WithMany ( )
				.Map ( m => m
					       .ToTable ( "TeleSubscriptionCoins" )
					       .MapLeftKey ( "SubscriptionId" )
					       .MapRightKey ( "CoinId" )
				);

			base.OnModelCreating ( modelBuilder );
		}
	}
}