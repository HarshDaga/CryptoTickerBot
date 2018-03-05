using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using NLog;

namespace CryptoTickerBot.Core
{
	public class GoogleSheetsService : IDisposable
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private static readonly string[] Scopes = {SheetsService.Scope.Spreadsheets};

		public SheetsService Service { get; }
		public CancellationTokenSource Cts { get; }

		public string ApplicationName { get; }
		public string SheetName { get; }
		public string SheetId { get; }

		private GoogleSheetsService (
			SheetsService service,
			CancellationTokenSource cts,
			string applicationName,
			string sheetName,
			string sheetId
		)
		{
			Service         = service;
			Cts             = cts;
			ApplicationName = applicationName;
			SheetName       = sheetName;
			SheetId         = sheetId;
		}

		public void Dispose ( ) => Service?.Dispose ( );

		private static UserCredential GetCredentials ( )
		{
			UserCredential credential;

			using ( var stream =
				new FileStream ( "client_secret.json", FileMode.Open, FileAccess.Read ) )
			{
				var credPath = Environment.GetFolderPath (
					Environment.SpecialFolder.Personal );
				credPath = Path.Combine ( credPath, ".credentials/sheets.googleapis.com-dotnet-quickstart.json" );

				credential = GoogleWebAuthorizationBroker.AuthorizeAsync (
					GoogleClientSecrets.Load ( stream ).Secrets,
					Scopes,
					"user",
					CancellationToken.None,
					new FileDataStore ( credPath, true ) ).Result;
				Logger.Info ( "Credential file saved to: " + credPath );
			}

			return credential;
		}

		public static GoogleSheetsService Build (
			CancellationTokenSource cts,
			string applicationName,
			string sheetName,
			string sheetId
		)
		{
			var credential = GetCredentials ( );

			var service = new SheetsService ( new BaseClientService.Initializer
			{
				HttpClientInitializer = credential,
				ApplicationName       = applicationName
			} );

			return new GoogleSheetsService ( service, cts, applicationName, sheetName, sheetId );
		}

		public async Task UpdateSheet ( IList<ValueRange> valueRanges )
		{
			try
			{
				var requestBody = new BatchUpdateValuesRequest
				{
					ValueInputOption = "USER_ENTERED",
					Data             = valueRanges
				};

				var request = Service.Spreadsheets.Values.BatchUpdate (
					requestBody,
					SheetId
				);

				await request.ExecuteAsync ( Cts.Token );
			}
			catch ( Exception e )
			{
				if ( e is GoogleApiException gae && gae.Error.Code == 429 )
				{
					Logger.Error ( gae, "Too many Google Api requests. Cooling down." );
					await Task.Delay ( 5000, Cts.Token );
				}
				else
				{
					Logger.Error ( e );
				}
			}
		}
	}
}