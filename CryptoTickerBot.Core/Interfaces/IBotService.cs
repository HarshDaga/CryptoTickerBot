using System;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Domain;

namespace CryptoTickerBot.Core.Interfaces
{
	public interface IBotService : IDisposable, IEquatable<IBotService>
	{
		Guid Guid { get; }
		IBot Bot { get; }
		bool IsAttached { get; }

		Task AttachTo ( IBot bot );
		Task Detach ( );

		Task OnNext ( ICryptoExchange exchange,
		              CryptoCoin coin );

		Task OnChanged ( ICryptoExchange exchange,
		                 CryptoCoin coin );
	}
}