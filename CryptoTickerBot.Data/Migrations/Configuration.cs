using System.Data.Entity.Migrations;
using CryptoTickerBot.Data.Persistence;

namespace CryptoTickerBot.Data.Migrations
{
	internal sealed class Configuration : DbMigrationsConfiguration<CtbContext>
	{
		public Configuration ( )
		{
			AutomaticMigrationsEnabled = true;
		}

		protected override void Seed ( CtbContext context )
		{
			CtbContextInitialzer.StaticSeed ( context );
		}
	}
}