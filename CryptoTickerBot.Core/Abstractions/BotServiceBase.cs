using System;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Interfaces;
using CryptoTickerBot.Data.Domain;

namespace CryptoTickerBot.Core.Abstractions
{
	public abstract class BotServiceBase : IBotService
	{
		public Guid Guid { get; } = Guid.NewGuid ( );
		public IBot Bot { get; protected set; }
		public bool IsAttached { get; protected set; }

		public virtual async Task AttachToAsync ( IBot bot )
		{
			if ( !bot.ContainsService ( this ) )
			{
				await bot.AttachAsync ( this ).ConfigureAwait ( false );
				return;
			}

			Bot        = bot;
			IsAttached = true;

			await StartAsync ( ).ConfigureAwait ( false );
		}

		public virtual async Task DetachAsync ( )
		{
			if ( Bot.ContainsService ( this ) )
			{
				await Bot.DetachAsync ( this ).ConfigureAwait ( false );
				return;
			}

			IsAttached = false;

			await StopAsync ( ).ConfigureAwait ( false );
		}

		public virtual Task OnNextAsync ( ICryptoExchange exchange,
		                                  CryptoCoin coin ) =>
			Task.CompletedTask;

		public virtual Task OnChangedAsync ( ICryptoExchange exchange,
		                                     CryptoCoin coin ) =>
			Task.CompletedTask;

		public virtual void Dispose ( )
		{
		}

		public bool Equals ( IBotService other ) =>
			Guid.Equals ( other?.Guid );

		public virtual Task StartAsync ( ) =>
			Task.CompletedTask;

		public virtual Task StopAsync ( ) =>
			Task.CompletedTask;

		public override bool Equals ( object obj )
		{
			if ( obj is null )
				return false;
			if ( ReferenceEquals ( this, obj ) )
				return true;
			if ( obj is IBotService service )
				return Equals ( service );
			return false;
		}

		public override int GetHashCode ( ) => Guid.GetHashCode ( );

		public static bool operator == ( BotServiceBase left,
		                                 BotServiceBase right ) => Equals ( left, right );

		public static bool operator != ( BotServiceBase left,
		                                 BotServiceBase right ) => !Equals ( left, right );
	}
}