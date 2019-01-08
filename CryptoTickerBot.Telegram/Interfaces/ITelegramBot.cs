using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CryptoTickerBot.Telegram.Interfaces
{
	public interface ITelegramBot
	{
		TelegramBotClient Client { get; }
		User Self { get; }
		TelegramBotConfig Config { get; }
		Policy Policy { get; }
		DateTime StartTime { get; }
		CancellationToken CancellationToken { get; }
		Task StartAsync ( );
		void Stop ( );
	}
}