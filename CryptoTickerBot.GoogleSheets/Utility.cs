using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CryptoTickerBot.Core;
using CryptoTickerBot.Core.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using NLog;

namespace CryptoTickerBot.GoogleSheets
{
	internal static class Utility
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		public static readonly string[] Scopes = {SheetsService.Scope.Spreadsheets};

		public static UserCredential GetCredentials ( )
		{
			using ( var stream =
				new FileStream ( "client_secret.json", FileMode.Open, FileAccess.Read ) )
			{
				var credPath = Path.Combine (
					Environment.GetFolderPath ( Environment.SpecialFolder.Personal ),
					".credentials/sheets.googleapis.com-dotnet-quickstart.json"
				);

				var credential = GoogleWebAuthorizationBroker.AuthorizeAsync (
					GoogleClientSecrets.Load ( stream ).Secrets,
					Scopes,
					"user",
					CancellationToken.None,
					new FileDataStore ( credPath, true ) ).Result;
				Logger.Info ( "Credential file saved to: " + credPath );

				return credential;
			}
		}

		public static IList<IList<object>> ToSheetsRows ( this ICryptoExchange exchange )
		{
			return ToSheetsRows ( exchange,
			                      coin => new object[]
			                      {
				                      coin.Symbol,
				                      coin.LowestAsk,
				                      coin.HighestBid,
				                      coin.Rate,
				                      $"{coin.Time:G}",
				                      coin.Spread,
				                      coin.SpreadPercentage
			                      } );
		}

		public static IList<IList<object>> ToSheetsRows ( this ICryptoExchange exchange,
		                                                  Func<CryptoCoin, IList<object>> selector )
		{
			return exchange.ExchangeData.Values
				.OrderBy ( coin => coin.Symbol )
				.Select ( selector )
				.Prepend ( new List<object> ( ) )
				.Prepend ( new List<object> {exchange.Name} )
				.ToList ( );
		}
	}
}