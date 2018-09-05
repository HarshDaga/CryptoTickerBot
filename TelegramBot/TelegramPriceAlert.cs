using System;
using CryptoTickerBot;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Exchanges.Core;
using JetBrains.Annotations;
using NLog;

namespace TelegramBot
{
	public class TelegramPriceAlert : CryptoExchangeSubscription
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		public Guid Id { get; }
		public long ChatId { get; }
		public string UserName { get; }
		public CryptoExchangeId ExchangeId { get; }
		public decimal Price { get; }
		public CryptoCoin InitialPrice { get; set; }
		public CryptoCoinId CoinId { get; }
		public bool HasTriggered { get; set; }

		public TelegramPriceAlert (
			CryptoExchangeBase exchange,
			long chatId,
			string userName,
			decimal price,
			CryptoCoinId coinId,
			CryptoCoin initialPrice = null
		) : base ( exchange )
		{
			Id           = Guid.NewGuid ( );
			ChatId       = chatId;
			UserName     = userName;
			ExchangeId   = exchange.Id;
			Price        = price;
			CoinId       = coinId;
			InitialPrice = ( initialPrice ?? exchange.ExchangeData[coinId] ).Clone ( );
		}

		public delegate void TelegramPriceAlertTriggeredDelegate (
			TelegramPriceAlert alert,
			CryptoCoin prevPrice,
			CryptoCoin newPrice
		);

		[UsedImplicitly]
		public event TelegramPriceAlertTriggeredDelegate Triggered;

		public override void OnNext ( CryptoCoin coin )
		{
			if ( !HasTriggered && CoinId != coin.Id )
				return;

			if ( InitialPrice.Rate < Price && coin.Rate >= Price )
				OnTriggered ( this, InitialPrice, coin );
			if ( InitialPrice.Rate > Price && coin.Rate <= Price )
				OnTriggered ( this, InitialPrice, coin );
		}

		protected virtual void OnTriggered ( TelegramPriceAlert alert,
		                                     CryptoCoin prevprice,
		                                     CryptoCoin newprice )
		{
			Triggered?.Invoke ( alert, prevprice, newprice );
			HasTriggered = true;
			Logger.Info (
				$"Invoked alert for {UserName} @ {newprice.Rate:C} {CoinId} {Exchange.Name}"
			);
		}
	}
}