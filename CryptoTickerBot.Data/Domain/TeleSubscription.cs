using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CryptoTickerBot.Data.Enums;
using Newtonsoft.Json;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace CryptoTickerBot.Data.Domain
{
	public class TeleSubscription
	{
		private Dictionary<CryptoCoinId, CryptoCoinValue> lastSignificantPrice;

		[Key]
		[DatabaseGenerated ( DatabaseGeneratedOption.Identity )]
		public int Id { get; set; }

		[ForeignKey ( nameof ( Exchange ) )]
		public CryptoExchangeId ExchangeId { get; set; }

		public CryptoExchange Exchange { get; set; }

		[Required]
		public long ChatId { get; set; }

		[Required]
		public string UserName { get; set; }

		[Required]
		public decimal Threshold { get; set; }

		public HashSet<CryptoCoin> Coins { get; set; }

		[StringLength ( 2000 )]
		public string LastSignificantPriceJson { get; set; }

		[Required]
		[DatabaseGenerated ( DatabaseGeneratedOption.None )]
		public DateTime StartDate { get; set; }

		[DatabaseGenerated ( DatabaseGeneratedOption.None )]
		public DateTime? EndDate { get; set; }

		public bool Expired { get; set; }

		[NotMapped]
		public Dictionary<CryptoCoinId, CryptoCoinValue> LastSignificantPrice
		{
			get => ParseJson ( );
			set => LastSignificantPriceJson = JsonConvert.SerializeObject ( value.Values );
		}

		private TeleSubscription ( )
		{
			LastSignificantPrice = new Dictionary<CryptoCoinId, CryptoCoinValue> ( );
			Expired              = false;
		}

		public TeleSubscription (
			CryptoExchangeId exchangeId,
			long chatId,
			string userName,
			decimal threshold,
			IEnumerable<CryptoCoin> coins,
			IDictionary<CryptoCoinId, CryptoCoinValue> lastSignificantPrice = null,
			DateTime? startDate = null,
			DateTime? endDate = null
		) : this ( )
		{
			ExchangeId = exchangeId;
			ChatId     = chatId;
			UserName   = userName;
			Threshold  = threshold;
			Coins      = new HashSet<CryptoCoin> ( coins );
			StartDate  = startDate ?? DateTime.UtcNow;
			EndDate    = endDate;

			if ( lastSignificantPrice != null )
				foreach ( var kp in lastSignificantPrice )
					LastSignificantPrice[kp.Key] = kp.Value;

			UpdateJson ( );
		}

		private Dictionary<CryptoCoinId, CryptoCoinValue> ParseJson ( )
		{
			List<CryptoCoinValue> values;
			try
			{
				values = JsonConvert
					.DeserializeObject<List<CryptoCoinValue>> ( LastSignificantPriceJson );
			}
			catch ( Exception )
			{
				values = new List<CryptoCoinValue> ( );
			}

			lastSignificantPrice = values.ToDictionary ( x => x.CoinId, x => x );

			return lastSignificantPrice;
		}

		public void UpdateJson ( )
		{
			if ( lastSignificantPrice != null )
				LastSignificantPriceJson = JsonConvert.SerializeObject ( lastSignificantPrice.Values );
		}
	}
}