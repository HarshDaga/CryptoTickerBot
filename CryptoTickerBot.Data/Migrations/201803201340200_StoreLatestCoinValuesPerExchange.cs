using System.Data.Entity.Migrations;

namespace CryptoTickerBot.Data.Migrations
{
	public partial class StoreLatestCoinValuesPerExchange : DbMigration
	{
		public override void Up ( )
		{
			CreateTable (
					"dbo.LatestCoinValues",
					c => new
					{
						ExchangeId  = c.Int ( false ),
						CoinValueId = c.Int ( false )
					} )
				.PrimaryKey ( t => new {t.ExchangeId, t.CoinValueId} )
				.ForeignKey ( "dbo.CryptoExchanges", t => t.ExchangeId )
				.ForeignKey ( "dbo.CryptoCoinValues", t => t.CoinValueId )
				.Index ( t => t.ExchangeId )
				.Index ( t => t.CoinValueId );
		}

		public override void Down ( )
		{
			DropForeignKey ( "dbo.LatestCoinValues", "CoinValueId", "dbo.CryptoCoinValues" );
			DropForeignKey ( "dbo.LatestCoinValues", "ExchangeId", "dbo.CryptoExchanges" );
			DropIndex ( "dbo.LatestCoinValues", new[] {"CoinValueId"} );
			DropIndex ( "dbo.LatestCoinValues", new[] {"ExchangeId"} );
			DropTable ( "dbo.LatestCoinValues" );
		}
	}
}