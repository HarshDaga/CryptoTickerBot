using System;
using CryptoTickerBot.Core.Interfaces;

namespace CryptoTickerBot.GoogleSheets
{
	public class SheetsConfig : IConfig
	{
		public string ConfigFileName { get; } = "SheetsConfig";

		public string SpreadSheetId { get; set; }
		public string SheetName { get; set; }
		public int SheetId { get; set; }
		public string ApplicationName { get; set; }
		public TimeSpan UpdateFrequency { get; set; } = TimeSpan.FromSeconds ( 6 );
		public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromSeconds ( 60 );

		public int StartingRow { get; set; } = 5;
		public char StartingColumn { get; set; } = 'A';
		public int ExchangeRowGap { get; set; } = 3;
	}
}