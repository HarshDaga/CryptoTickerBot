﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Core;
using CryptoTickerBot.Data.Enums;
using CryptoTickerBot.Data.Persistence;
using CryptoTickerBot.Exchanges.Core;
using CryptoTickerBot.Helpers;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using TelegramBot.Extensions;

namespace TelegramBot.CryptoTickerTeleBot
{
	public partial class TeleBot
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		private readonly Dictionary<string, (UserRole role, MessageHandlerDelegate func)> commands;

		private readonly Bot ctb;

		private readonly Dictionary<CryptoExchangeId, CryptoExchangeBase> exchanges;

		private TelegramBotClient bot;
		private User me;

		public string BotToken { get; }
		public List<TeleBotUser> Users { get; private set; }

		public TeleBot ( string botToken, Bot ctb )
		{
			BotToken      = botToken;
			Subscriptions = new List<TelegramSubscription> ( );
			this.ctb      = ctb;
			exchanges     = ctb.Exchanges;

			FetchUserList ( );

			commands = new
				Dictionary<string, (UserRole role, MessageHandlerDelegate func)>
				{
					["/status"]      = ( UserRole.Guest, HandleStatus ),
					["/fetch"]       = ( UserRole.Guest, HandleFetch ),
					["/compare"]     = ( UserRole.Registered, HandleCompare ),
					["/best"]        = ( UserRole.Registered, HandleBest ),
					["/subscribe"]   = ( UserRole.Registered, HandleSubscribe ),
					["/unsubscribe"] = ( UserRole.Registered, HandleUnsubscribe ),
					["/restart"]     = ( UserRole.Admin, HandleRestart ),
					["/users"]       = ( UserRole.Admin, HandleUsers ),
					["/kill"]        = ( UserRole.Admin, HandleKill ),
					["/putgroup"]    = ( UserRole.Owner, HandlePutGroup )
				};
		}

		private void FetchUserList ( )
		{
			using ( var unit = new UnitOfWork ( ) )
			{
				Users = unit.Users.GetAll ( )
					.Select ( x => new TeleBotUser ( x.UserName, x.Role, x.Created ) )
					.ToList ( );
				unit.Complete ( );
			}
		}

		public void Start ( )
		{
			try
			{
				bot                =  new TelegramBotClient ( BotToken );
				bot.OnMessage      += BotClientOnMessage;
				bot.OnInlineQuery  += BotClientOnInlineQuery;
				bot.OnReceiveError += BotClientOnReceiveError;

				me = bot.GetMeAsync ( ).Result;
				Logger.Info ( $"Hello! My name is {me.FirstName}" );

				bot.StartReceiving ( );

				while ( !ctb.IsInitialized )
					Thread.Sleep ( 10 );

				LoadSubscriptions ( );
				SendResumeNotifications ( );

				Console.ReadLine ( );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
				throw;
			}
		}

		private static void BotClientOnReceiveError ( object sender, ReceiveErrorEventArgs receiveErrorEventArgs )
		{
			Logger.Error (
				receiveErrorEventArgs.ApiRequestException,
				$"Error Code: {receiveErrorEventArgs.ApiRequestException.ErrorCode}"
			);
		}

		private async void BotClientOnInlineQuery ( object sender, InlineQueryEventArgs eventArgs )
		{
			var userName = eventArgs.InlineQuery.From.Username;
			Logger.Debug ( $"Received inline query from: {userName}" );
			if ( !Users.Contains ( userName ) )
			{
				var user = new TeleBotUser ( userName );
				Users.Add ( user );
				using ( var unit = new UnitOfWork ( ) )
				{
					unit.Users.Add ( user.UserName, user.Role, user.Created );
					unit.Complete ( );
				}
			}

			var fiat = eventArgs.InlineQuery.Query.ToFiatCurrency ( );
			var inlineQueryResults = exchanges.Values
				.Select ( x => ToInlineQueryResult ( x, x.Name, fiat ) )
				.ToList<InlineQueryResult> ( );

			try
			{
				await bot.AnswerInlineQueryAsync (
					eventArgs.InlineQuery.Id,
					inlineQueryResults.ToArray ( ),
					0
				);
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		private static InlineQueryResultArticle ToInlineQueryResult (
			CryptoExchangeBase exchange,
			string name,
			FiatCurrency fiat = FiatCurrency.USD
		) =>
			new InlineQueryResultArticle
			{
				Id      = name,
				HideUrl = true,
				Title   = name,
				Url     = exchange.Url,
				InputMessageContent = new InputTextMessageContent
				{
					MessageText = $"```\n{name}\n{exchange.ToTable ( fiat )}\n```",
					ParseMode   = ParseMode.Markdown
				}
			};

		private async void BotClientOnMessage ( object sender, MessageEventArgs messageEventArgs )
		{
			try
			{
				var message = messageEventArgs.Message;

				if ( message == null || message.Type != MessageType.TextMessage )
					return;

				ParseMessage ( message, out var command, out var parameters, out var userName );
				Logger.Debug ( $"Message received from {userName}: {message.Text}" );

				if ( await ValidateUserCommand ( userName, command, message ) )
					return;

				await commands[command].func ( message, parameters );
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		private delegate Task MessageHandlerDelegate ( Message message, IList<string> @params );
	}
}