using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Data.Domain;
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

		public static UserCredential GetCredentials ( string clientSecretPath,
		                                              string credentialsPath )
		{
			using ( var stream =
				new FileStream ( clientSecretPath, FileMode.Open, FileAccess.Read ) )
			{
				credentialsPath = Path.GetFullPath ( credentialsPath );

				var credential = GoogleWebAuthorizationBroker.AuthorizeAsync (
					GoogleClientSecrets.Load ( stream ).Secrets,
					Scopes,
					"user",
					CancellationToken.None,
					new FileDataStore ( Path.GetFullPath ( credentialsPath ), true ) ).Result;
				Logger.Info ( "Credential file saved to: " + credentialsPath );

				return credential;
			}
		}

		public static IList<IList<object>> ToSheetsRows ( this ICryptoExchange exchange )
		{
			return ToSheetsRows ( exchange,
			                      coin => new object[]
			                      {
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
			return exchange
				.Markets
				.BaseSymbols
				.OrderBy ( x => x )
				.SelectMany ( x => exchange
					              .Markets
					              .Data[x]
					              .OrderBy ( y => y.Key )
					              .Select ( y => (IList<object>) selector ( y.Value )
						                        .Prepend ( x )
						                        .Prepend ( y.Key )
						                        .ToList ( ) ) )
				.Prepend ( new List<object> ( ) )
				.Prepend ( new List<object> {$"{exchange.Name} ({exchange.Count})"} )
				.ToList ( );
		}
	}
}