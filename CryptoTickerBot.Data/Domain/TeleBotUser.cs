using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CryptoTickerBot.Data.Enums;
using JetBrains.Annotations;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace CryptoTickerBot.Data.Domain
{
	public class TeleBotUser
	{
		[Key]
		public string UserName { get; set; }

		[Required]
		public UserRole Role { get; set; }

		[Required]
		[DatabaseGenerated ( DatabaseGeneratedOption.None )]
		public DateTime Created { get; set; }

		public TeleBotUser ( string userName, UserRole role, DateTime? created = null )
		{
			UserName = userName;
			Role     = role;
			Created  = created ?? DateTime.UtcNow;
		}

		[UsedImplicitly]
		private TeleBotUser ( )
		{
		}

		public override string ToString ( ) =>
			$"{UserName} {Role} {Created:g}";
	}
}