using System.Data.Entity;
using CryptoTickerBot.Data.Domain;
using CryptoTickerBot.Data.Migrations;
using CryptoTickerBot.Data.Persistence.Configurations;
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
			Database.SetInitializer (
				new MigrateDatabaseToLatestVersion<CtbContext, Configuration> ( )
			);
		}

		protected override void OnModelCreating ( DbModelBuilder modelBuilder )
		{
			Logger.Debug ( "Creating Database model" );

			modelBuilder.Configurations.Add ( new CryptoExchangeConfiguration ( ) );
			modelBuilder.Configurations.Add ( new TeleSubscriptionConfiguration ( ) );

			modelBuilder.Entity<WithdrawalFees> ( )
				.Property ( e => e.Value )
				.HasPrecision ( 18, 6 );

			modelBuilder.Entity<DepositFees> ( )
				.Property ( e => e.Value )
				.HasPrecision ( 18, 6 );

			base.OnModelCreating ( modelBuilder );
		}
	}
}