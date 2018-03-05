using System.ComponentModel.DataAnnotations;
using CryptoTickerBot.Data.Enums;
using JetBrains.Annotations;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace CryptoTickerBot.Data.Domain
{
	public class CryptoCoin
	{
		[Key]
		public CryptoCoinId Id { get; set; }

		[Required]
		public string Symbol { get; set; }

		public string Name { get; set; }

		public CryptoCoin ( CryptoCoinId id, string symbol, string name )
		{
			Id     = id;
			Symbol = symbol;
			Name   = name;
		}

		public CryptoCoin ( CryptoCoinId id, string name )
		{
			Id     = id;
			Symbol = id.ToString ( );
			Name   = name;
		}

		[UsedImplicitly]
		private CryptoCoin ( )
		{
		}

		public override string ToString ( ) => $"{Symbol}";
	}
}