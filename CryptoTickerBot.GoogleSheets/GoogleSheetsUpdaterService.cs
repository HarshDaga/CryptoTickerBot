using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Data.Domain;
using Google;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using JetBrains.Annotations;
using NLog;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum;

namespace CryptoTickerBot.GoogleSheets
{
	public class GoogleSheetsUpdaterService : BotServiceBase
	{
		public const string FolderName = "GoogleApi";
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public SheetsConfig Config { get; }

		public SheetsService Service { get; }

		public DateTime LastUpdate { get; private set; } = DateTime.UtcNow;

		private int previousCount;

		public GoogleSheetsUpdaterService ( SheetsConfig config )
		{
			Config = config;

			try
			{
				var credential = Utility.GetCredentials (
					Path.Combine ( FolderName, "ClientSecret.json" ),
					Path.Combine ( FolderName, "Credentials" )
				);

				Service = new SheetsService ( new BaseClientService.Initializer
				{
					HttpClientInitializer = credential,
					ApplicationName       = config.ApplicationName
				} );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		[UsedImplicitly]
		public event UpdateDelegate Update;

		public override async Task OnChangedAsync ( ICryptoExchange exchange,
		                                            CryptoCoin coin )
		{
			if ( DateTime.UtcNow - LastUpdate < Config.UpdateFrequency )
				return;
			LastUpdate = DateTime.UtcNow;

			try
			{
				var valueRanges = GetValueRangeToUpdate ( );
				await UpdateSheetAsync ( valueRanges ).ConfigureAwait ( false );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		private async Task ClearSheetAsync ( )
		{
			var requestBody = new BatchUpdateSpreadsheetRequest
			{
				Requests = new List<Request>
				{
					new Request
					{
						UpdateCells = new UpdateCellsRequest
						{
							Range  = new GridRange {SheetId = Config.SheetId},
							Fields = "userEnteredValue"
						}
					}
				}
			};

			var request = Service.Spreadsheets.BatchUpdate ( requestBody, Config.SpreadSheetId );

			await request.ExecuteAsync ( Bot.Cts.Token ).ConfigureAwait ( false );
		}

		private async Task UpdateSheetAsync ( ValueRange valueRange )
		{
			try
			{
				if ( valueRange.Values.Count != previousCount )
					await ClearSheetAsync ( ).ConfigureAwait ( false );
				previousCount = valueRange.Values.Count;

				var request = Service.Spreadsheets.Values.Update ( valueRange, Config.SpreadSheetId, valueRange.Range );
				request.ValueInputOption = USERENTERED;

				await request.ExecuteAsync ( Bot.Cts.Token ).ConfigureAwait ( false );

				Update?.Invoke ( this );
			}
			catch ( Exception e )
			{
				await HandleUpdateExceptionAsync ( e ).ConfigureAwait ( false );
			}
		}

		private async Task HandleUpdateExceptionAsync ( Exception e )
		{
			if ( e is GoogleApiException gae && gae.Error?.Code == 429 )
			{
				Logger.Warn ( gae, "Too many Google Api requests. Cooling down." );
				await Task.Delay ( Config.CooldownPeriod, Bot.Cts.Token ).ConfigureAwait ( false );
			}
			else if ( !Bot.Cts.IsCancellationRequested &&
			          ( e is TaskCanceledException ||
			            e is OperationCanceledException ) )
			{
				Logger.Warn ( e );
			}
			else
			{
				Logger.Error ( e );
			}
		}

		[Pure]
		private ValueRange GetValueRangeToUpdate ( )
		{
			var start = Config.StartingRow;
			var firstColumn = Config.StartingColumn;
			var lastColumn = (char) ( Config.StartingColumn + 8 );

			var rows = new List<IList<object>> ( );
			foreach ( var exchange in Bot.Exchanges.Values.OrderBy ( x => x.Name ) )
			{
				rows.AddRange ( exchange.ToSheetsRows ( ) );
				for ( var i = 0; i < Config.ExchangeRowGap; i++ )
					rows.Add ( new List<object> ( ) );
			}

			return new ValueRange
			{
				Values = rows,
				Range  = $"{Config.SheetName}!{firstColumn}{start}:{lastColumn}{start + rows.Count}"
			};
		}

		public override void Dispose ( ) => Service?.Dispose ( );
	}

	public delegate Task UpdateDelegate ( GoogleSheetsUpdaterService service );
}