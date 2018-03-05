using System.Data.Entity.Migrations;

namespace CryptoTickerBot.Data.Migrations
{
	public partial class InitialMigration : DbMigration
	{
		public override void Up ( )
		{
			CreateTable (
					"dbo.CryptoCoins",
					c => new
					{
						Id     = c.Int ( false ),
						Symbol = c.String ( false ),
						Name   = c.String ( )
					} )
				.PrimaryKey ( t => t.Id );

			CreateTable (
					"dbo.CryptoCoinValues",
					c => new
					{
						Id         = c.Int ( false, true ),
						CoinId     = c.Int ( false ),
						ExchangeId = c.Int ( false ),
						LowestAsk  = c.Decimal ( false, 18, 2 ),
						HighestBid = c.Decimal ( false, 18, 2 ),
						Time       = c.DateTime ( false )
					} )
				.PrimaryKey ( t => t.Id )
				.ForeignKey ( "dbo.CryptoCoins", t => t.CoinId, true )
				.ForeignKey ( "dbo.CryptoExchanges", t => t.ExchangeId, true )
				.Index ( t => t.CoinId )
				.Index ( t => t.ExchangeId )
				.Index ( t => t.Time );

			CreateTable (
					"dbo.CryptoExchanges",
					c => new
					{
						Id         = c.Int ( false ),
						Name       = c.String ( false ),
						Url        = c.String ( false ),
						TickerUrl  = c.String ( false ),
						BuyFees    = c.Decimal ( false, 18, 2 ),
						SellFees   = c.Decimal ( false, 18, 2 ),
						LastUpdate = c.DateTime ( ),
						LastChange = c.DateTime ( )
					} )
				.PrimaryKey ( t => t.Id );

			CreateTable (
					"dbo.DepositFees",
					c => new
					{
						ExchangeId = c.Int ( false ),
						CoinId     = c.Int ( false ),
						Value      = c.Decimal ( false, 18, 6 )
					} )
				.PrimaryKey ( t => new {t.ExchangeId, t.CoinId} )
				.ForeignKey ( "dbo.CryptoCoins", t => t.CoinId, true )
				.ForeignKey ( "dbo.CryptoExchanges", t => t.ExchangeId, true )
				.Index ( t => t.ExchangeId )
				.Index ( t => t.CoinId );

			CreateTable (
					"dbo.WithdrawalFees",
					c => new
					{
						ExchangeId = c.Int ( false ),
						CoinId     = c.Int ( false ),
						Value      = c.Decimal ( false, 18, 6 )
					} )
				.PrimaryKey ( t => new {t.ExchangeId, t.CoinId} )
				.ForeignKey ( "dbo.CryptoCoins", t => t.CoinId, true )
				.ForeignKey ( "dbo.CryptoExchanges", t => t.ExchangeId, true )
				.Index ( t => t.ExchangeId )
				.Index ( t => t.CoinId );

			CreateTable (
					"dbo.TeleBotUsers",
					c => new
					{
						UserName = c.String ( false, 128 ),
						Role     = c.Int ( false ),
						Created  = c.DateTime ( false )
					} )
				.PrimaryKey ( t => t.UserName );

			CreateTable (
					"dbo.TeleSubscriptions",
					c => new
					{
						Id                       = c.Int ( false, true ),
						ExchangeId               = c.Int ( false ),
						ChatId                   = c.Long ( false ),
						UserName                 = c.String ( false ),
						Threshold                = c.Decimal ( false, 18, 4 ),
						LastSignificantPriceJson = c.String ( maxLength: 2000 ),
						StartDate                = c.DateTime ( false ),
						EndDate                  = c.DateTime ( ),
						Expired                  = c.Boolean ( false )
					} )
				.PrimaryKey ( t => t.Id )
				.ForeignKey ( "dbo.CryptoExchanges", t => t.ExchangeId, true )
				.Index ( t => t.ExchangeId );

			CreateTable (
					"dbo.TeleSubscriptionCoins",
					c => new
					{
						SubscriptionId = c.Int ( false ),
						CoinId         = c.Int ( false )
					} )
				.PrimaryKey ( t => new {t.SubscriptionId, t.CoinId} )
				.ForeignKey ( "dbo.TeleSubscriptions", t => t.SubscriptionId, true )
				.ForeignKey ( "dbo.CryptoCoins", t => t.CoinId, true )
				.Index ( t => t.SubscriptionId )
				.Index ( t => t.CoinId );
		}

		public override void Down ( )
		{
			DropForeignKey ( "dbo.TeleSubscriptions", "ExchangeId", "dbo.CryptoExchanges" );
			DropForeignKey ( "dbo.TeleSubscriptionCoins", "CoinId", "dbo.CryptoCoins" );
			DropForeignKey ( "dbo.TeleSubscriptionCoins", "SubscriptionId", "dbo.TeleSubscriptions" );
			DropForeignKey ( "dbo.WithdrawalFees", "ExchangeId", "dbo.CryptoExchanges" );
			DropForeignKey ( "dbo.WithdrawalFees", "CoinId", "dbo.CryptoCoins" );
			DropForeignKey ( "dbo.DepositFees", "ExchangeId", "dbo.CryptoExchanges" );
			DropForeignKey ( "dbo.DepositFees", "CoinId", "dbo.CryptoCoins" );
			DropForeignKey ( "dbo.CryptoCoinValues", "ExchangeId", "dbo.CryptoExchanges" );
			DropForeignKey ( "dbo.CryptoCoinValues", "CoinId", "dbo.CryptoCoins" );
			DropIndex ( "dbo.TeleSubscriptionCoins", new[] {"CoinId"} );
			DropIndex ( "dbo.TeleSubscriptionCoins", new[] {"SubscriptionId"} );
			DropIndex ( "dbo.TeleSubscriptions", new[] {"ExchangeId"} );
			DropIndex ( "dbo.WithdrawalFees", new[] {"CoinId"} );
			DropIndex ( "dbo.WithdrawalFees", new[] {"ExchangeId"} );
			DropIndex ( "dbo.DepositFees", new[] {"CoinId"} );
			DropIndex ( "dbo.DepositFees", new[] {"ExchangeId"} );
			DropIndex ( "dbo.CryptoCoinValues", new[] {"Time"} );
			DropIndex ( "dbo.CryptoCoinValues", new[] {"ExchangeId"} );
			DropIndex ( "dbo.CryptoCoinValues", new[] {"CoinId"} );
			DropTable ( "dbo.TeleSubscriptionCoins" );
			DropTable ( "dbo.TeleSubscriptions" );
			DropTable ( "dbo.TeleBotUsers" );
			DropTable ( "dbo.WithdrawalFees" );
			DropTable ( "dbo.DepositFees" );
			DropTable ( "dbo.CryptoExchanges" );
			DropTable ( "dbo.CryptoCoinValues" );
			DropTable ( "dbo.CryptoCoins" );
		}
	}
}