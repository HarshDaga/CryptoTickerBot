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
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public int Id { get; set; }

		public string UserName { get; set; }

		[Required]
		public UserRole Role { get; set; }

		[Required]
		[DatabaseGenerated ( DatabaseGeneratedOption.None )]
		public DateTime Created { get; set; }

		public TeleBotUser ( int id, string userName, UserRole role, DateTime? created = null )
		{
			Id       = id;
			UserName = userName;
			Role     = role;
			Created  = created ?? DateTime.UtcNow;
		}

		[UsedImplicitly]
		private TeleBotUser ( )
		{
		}

		public override string ToString ( ) =>
			$"{Id,-10} {UserName} {Role} {Created:g}";
	}
}