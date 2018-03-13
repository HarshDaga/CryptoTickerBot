using System;
using System.Threading;
using CryptoTickerBot.WebSocket.Services;
using NLog;
using TelegramBot;
using TelegramBot.Core;
using WebSocketSharp.Server;
using CTB = CryptoTickerBot.Core.CryptoTickerBot;
using LogLevel = WebSocketSharp.LogLevel;

namespace CryptoTickerBot.WebSocket
{
	public class Program
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public static void Main ( )
		{
			AppDomain.CurrentDomain.UnhandledException += ( sender, args ) =>
				Logger.Error ( args );

			Console.Title = "Crypto Ticker Bot";

			var cts = new CancellationTokenSource ( );
			var ctb = CTB.CreateAndStart (
				cts,
				Settings.Instance.ApplicationName,
				Settings.Instance.SheetName,
				Settings.Instance.SheetId,
				Settings.Instance.SheetsRanges
			);

			var teleBot = new TeleBot ( Settings.Instance.BotToken, ctb );
			teleBot.Start ( );

			var sv = new WebSocketServer ( "ws://localhost:20421" );
			sv.Log.Level = LogLevel.Trace;
			sv.AddWebSocketService (
				"/telebot",
				( ) => new TeleBotWebSocketService ( teleBot )
			);
			sv.Start ( );

			Console.ReadLine ( );
		}
	}
}