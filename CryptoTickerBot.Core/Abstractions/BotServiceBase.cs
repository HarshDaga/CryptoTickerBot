using System;
using System.Threading.Tasks;
using CryptoTickerBot.Core.Interfaces;

namespace CryptoTickerBot.Core.Abstractions
{
	public abstract class BotServiceBase : IBotService
	{
		public Guid Guid { get; } = Guid.NewGuid ( );
		public IBot Bot { get; protected set; }
		public bool IsAttached { get; protected set; }

		public virtual async Task AttachTo ( IBot bot )
		{
			if ( !bot.ContainsService ( this ) )
			{
				await bot.Attach ( this );
				return;
			}

			Bot        = bot;
			IsAttached = true;

			await StartAsync ( );
		}

		public virtual async Task Detach ( )
		{
			if ( Bot.ContainsService ( this ) )
			{
				await Bot.Detach ( this );
				return;
			}

			IsAttached = false;

			await StopAsync ( );
		}

		public virtual Task OnNext ( ICryptoExchange exchange,
		                             CryptoCoin coin ) =>
			Task.CompletedTask;

		public virtual Task OnChanged ( ICryptoExchange exchange,
		                                CryptoCoin coin ) =>
			Task.CompletedTask;

		public virtual void Dispose ( )
		{
		}

		public bool Equals ( IBotService other ) => Guid.Equals ( other?.Guid );

		public virtual Task StartAsync ( ) =>
			Task.CompletedTask;

		public virtual Task StopAsync ( ) =>
			Task.CompletedTask;

		public override bool Equals ( object obj )
		{
			if ( ReferenceEquals ( null, obj ) ) return false;
			if ( ReferenceEquals ( this, obj ) ) return true;
			if ( obj.GetType ( ) != GetType ( ) ) return false;
			return Equals ( (IBotService) obj );
		}

		public override int GetHashCode ( ) => Guid.GetHashCode ( );

		public static bool operator == ( BotServiceBase left,
		                                 BotServiceBase right ) => Equals ( left, right );

		public static bool operator != ( BotServiceBase left,
		                                 BotServiceBase right ) => !Equals ( left, right );
	}
}