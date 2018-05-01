using System;
using CryptoTickerBot.GoogleSheets;
using CryptoTickerBot.WebSocket.Services;
using NLog;
using TelegramBot.Core;
using WebSocketSharp.Server;
using LogLevel = WebSocketSharp.LogLevel;

// ReSharper disable UnusedVariable

namespace CryptoTickerBot.WebSocket
{
	public class Program
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public static void Main ( )
		{
			AppDomain.CurrentDomain.UnhandledException += ( sender, args ) =>
				Logger.Error ( args );

			LogManager.Configuration.Variables["DataSource"] = Data.Settings.Instance.DataSource;

			Console.Title = "Crypto Ticker Bot";

			var ctb = CryptoTickerBotCore.CreateAndStart ( );

			StartGoogleSheetUpdater ( ctb );

			var teleBot = new TeleBot ( Settings.Instance.BotToken, ctb );
			teleBot.Start ( );
			teleBot.Restart += bot => StartGoogleSheetUpdater ( bot.Ctb );

			var server = StartWebSocketServer ( teleBot );

			Console.ReadLine ( );
		}

		private static WebSocketServer StartWebSocketServer ( TeleBot teleBot )
		{
			try
			{
				var url = $"ws://{Settings.Instance.Ip}:{Settings.Instance.Port}";
				var sv = new WebSocketServer ( url );
				sv.Log.Level = LogLevel.Fatal;
				sv.AddWebSocketService (
					"/telebot",
					( ) => new TeleBotWebSocketService ( teleBot )
				);
				sv.Start ( );

				Logger.Info ( $"WebSocket Server started on {url}" );

				return sv;
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
				throw;
			}
		}

		public static GoogleSheetsUpdater StartGoogleSheetUpdater ( CryptoTickerBotCore ctb ) =>
			GoogleSheetsUpdater.Build (
				ctb,
				Settings.Instance.ApplicationName,
				Settings.Instance.SheetName,
				Settings.Instance.SheetId,
				Settings.Instance.SheetsRanges,
				Settings.Instance.SheetUpdateFrequency
			);
	}
}