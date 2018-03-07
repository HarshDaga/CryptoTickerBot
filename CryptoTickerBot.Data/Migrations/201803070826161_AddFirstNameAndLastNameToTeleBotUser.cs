namespace CryptoTickerBot.Data.Migrations
{
	using System.Data.Entity.Migrations;

	public partial class AddFirstNameAndLastNameToTeleBotUser : DbMigration
	{
		public override void Up ( )
		{
			AddColumn ( "dbo.TeleBotUsers", "FirstName", c => c.String ( ) );
			AddColumn ( "dbo.TeleBotUsers", "LastName", c => c.String ( ) );
		}

		public override void Down ( )
		{
			DropColumn ( "dbo.TeleBotUsers", "LastName" );
			DropColumn ( "dbo.TeleBotUsers", "FirstName" );
		}
	}
}