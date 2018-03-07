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
		[DatabaseGenerated ( DatabaseGeneratedOption.None )]
		public int Id { get; set; }

		[Required]
		public UserRole Role { get; set; }

		public string UserName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }

		[Required]
		[DatabaseGenerated ( DatabaseGeneratedOption.None )]
		public DateTime Created { get; set; }

		public TeleBotUser (
			int id,
			UserRole role,
			string userName,
			string firstName,
			string lastName = null,
			DateTime? created = null
		)
		{
			Id        = id;
			Role      = role;
			UserName  = userName;
			FirstName = firstName;
			LastName  = lastName;
			Created   = created ?? DateTime.UtcNow;
		}

		[UsedImplicitly]
		private TeleBotUser ( )
		{
		}

		public override string ToString ( ) =>
			$"{Id,-10} {Role,-12} {FirstName} {LastName} {Created:g}";
	}
}