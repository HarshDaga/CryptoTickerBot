using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTickerBot.Core;
using CryptoTickerBot.Core.Abstractions;
using CryptoTickerBot.Core.Interfaces;
using Google;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using JetBrains.Annotations;
using NLog;

namespace CryptoTickerBot.GoogleSheets
{
	public class GoogleSheetsUpdaterService : BotServiceBase
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public SheetsConfig Config { get; }

		public SheetsService Service { get; }

		public DateTime LastUpdate { get; private set; } = DateTime.UtcNow;

		private int previousCount;

		public GoogleSheetsUpdaterService ( SheetsConfig config )
		{
			Config = config;

			var credential = Utility.GetCredentials ( );

			Service = new SheetsService ( new BaseClientService.Initializer
			{
				HttpClientInitializer = credential,
				ApplicationName       = config.ApplicationName
			} );
		}

		[UsedImplicitly]
		public event UpdateDelegate Update;

		public override async Task OnChanged ( ICryptoExchange exchange,
		                                       CryptoCoin coin )
		{
			if ( DateTime.UtcNow - LastUpdate < Config.UpdateFrequency )
				return;
			LastUpdate = DateTime.UtcNow;

			try
			{
				var valueRanges = GetValueRangeToUpdate ( );
				await UpdateSheet ( valueRanges ).ConfigureAwait ( false );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		private async Task ClearSheet ( )
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

		private async Task UpdateSheet ( ValueRange valueRange )
		{
			try
			{
				if ( valueRange.Values.Count != previousCount )
					await ClearSheet ( );
				previousCount = valueRange.Values.Count;

				var request = Service.Spreadsheets.Values.Update ( valueRange, Config.SpreadSheetId, valueRange.Range );
				request.ValueInputOption =
					SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

				await request.ExecuteAsync ( Bot.Cts.Token ).ConfigureAwait ( false );

				Update?.Invoke ( this );
			}
			catch ( TaskCanceledException tce )
			{
				if ( !Bot.Cts.IsCancellationRequested )
					Logger.Warn ( tce );
			}
			catch ( Exception e )
			{
				if ( e is GoogleApiException gae && gae.Error?.Code == 429 )
				{
					Logger.Warn ( gae, "Too many Google Api requests. Cooling down." );
					await Task.Delay ( Config.CooldownPeriod, Bot.Cts.Token ).ConfigureAwait ( false );
				}
				else
				{
					Logger.Error ( e );
				}
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