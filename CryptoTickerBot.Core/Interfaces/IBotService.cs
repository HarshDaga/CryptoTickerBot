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

		Task AttachToAsync ( IBot bot );
		Task DetachAsync ( );

		Task OnNextAsync ( ICryptoExchange exchange,
		                   CryptoCoin coin );

		Task OnChangedAsync ( ICryptoExchange exchange,
		                      CryptoCoin coin );
	}
}