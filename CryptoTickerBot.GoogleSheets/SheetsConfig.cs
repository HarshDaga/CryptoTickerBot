using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTickerBot.Data.Configs;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace CryptoTickerBot.GoogleSheets
{
	public class SheetsConfig : IConfig<SheetsConfig>
	{
		public string ConfigFileName { get; } = "Sheets";
		public string ConfigFolderName { get; } = "Configs";

		public string SpreadSheetId { get; set; }
		public string SheetName { get; set; }
		public int SheetId { get; set; }
		public string ApplicationName { get; set; }
		public TimeSpan UpdateFrequency { get; set; } = TimeSpan.FromSeconds ( 6 );
		public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromSeconds ( 60 );

		public int StartingRow { get; set; } = 5;
		public char StartingColumn { get; set; } = 'A';
		public int ExchangeRowGap { get; set; } = 3;

		public SheetsConfig RestoreDefaults ( ) =>
			new SheetsConfig
			{
				SpreadSheetId   = SpreadSheetId,
				SheetName       = SheetName,
				SheetId         = SheetId,
				ApplicationName = ApplicationName
			};

		public bool Validate ( out IList<Exception> exceptions )
		{
			exceptions = new List<Exception> ( );

			if ( string.IsNullOrEmpty ( SpreadSheetId ) )
				exceptions.Add ( new ArgumentException ( "SpreadSheet ID missing", nameof ( SpreadSheetId ) ) );
			if ( string.IsNullOrEmpty ( SheetName ) )
				exceptions.Add ( new ArgumentException ( "SheetName missing", nameof ( SheetName ) ) );
			if ( string.IsNullOrEmpty ( ApplicationName ) )
				exceptions.Add ( new ArgumentException ( "Application Name missing", nameof ( ApplicationName ) ) );

			return !exceptions.Any ( );
		}
	}
}