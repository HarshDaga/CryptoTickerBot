using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CryptoTickerBot.Data.Enums;
using Newtonsoft.Json;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace CryptoTickerBot.Data.Domain
{
	public class TeleSubscription
	{
		private ObservableConcurrentDictionary<CryptoCoinId, CryptoCoinValue> lastSignificantPrice;

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
		public ObservableConcurrentDictionary<CryptoCoinId, CryptoCoinValue> LastSignificantPrice
		{
			get
			{
				ParseJson ( );

				return lastSignificantPrice;
			}
			set =>
				LastSignificantPriceJson = JsonConvert.SerializeObject ( value.Values );
		}

		private TeleSubscription ( )
		{
			LastSignificantPrice = new ObservableConcurrentDictionary<CryptoCoinId, CryptoCoinValue> ( );
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
		}

		private void ParseJson ( )
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

			lastSignificantPrice = new ObservableConcurrentDictionary<CryptoCoinId, CryptoCoinValue> ( );
			foreach ( var value in values )
				lastSignificantPrice[value.CoinId] = value;

			lastSignificantPrice.PropertyChanged += ( sender, args ) =>
				LastSignificantPriceJson = JsonConvert.SerializeObject (
					( (ObservableConcurrentDictionary<CryptoCoinId, CryptoCoinValue>) sender ).Values
				);
			lastSignificantPrice.CollectionChanged += ( sender, args ) =>
				LastSignificantPriceJson = JsonConvert.SerializeObject (
					( (ObservableConcurrentDictionary<CryptoCoinId, CryptoCoinValue>) sender ).Values
				);
		}
	}
}