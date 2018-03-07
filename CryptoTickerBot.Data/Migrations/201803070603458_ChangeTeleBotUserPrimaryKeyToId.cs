namespace CryptoTickerBot.Data.Migrations
{
	using System;
	using System.Data.Entity.Migrations;

	public partial class ChangeTeleBotUserPrimaryKeyToId : DbMigration
	{
		public override void Up ( )
		{
			DropPrimaryKey ( "dbo.TeleBotUsers" );
			AddColumn ( "dbo.TeleBotUsers", "Id", c => c.Int ( nullable: false ) );
			AlterColumn ( "dbo.TeleBotUsers", "UserName", c => c.String ( ) );
			AddPrimaryKey ( "dbo.TeleBotUsers", "Id" );
		}

		public override void Down ( )
		{
			DropPrimaryKey ( "dbo.TeleBotUsers" );
			AlterColumn ( "dbo.TeleBotUsers", "UserName", c => c.String ( nullable: false, maxLength: 128 ) );
			DropColumn ( "dbo.TeleBotUsers", "Id" );
			AddPrimaryKey ( "dbo.TeleBotUsers", "UserName" );
		}
	}
}