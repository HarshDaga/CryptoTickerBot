using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTickerBot.Telegram.Interfaces;
using CryptoTickerBot.Telegram.Menus.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

#pragma warning disable 1998

namespace CryptoTickerBot.Telegram.Menus.Pages
{
	internal class SelectionPage<T> : PageBase
	{
		public T Result { get; private set; }
		public bool UserChoseBack { get; private set; }
		public Func<T, string> ToStringConverter { get; }
		private readonly Dictionary<string, T> selectionMap = new Dictionary<string, T> ( );

		public SelectionPage ( IMenu menu,
		                       IEnumerable<IEnumerable<T>> rows,
		                       IPage previousPage = null,
		                       string title = "Choose",
		                       Func<T, string> toStringConverter = null ) :
			base ( title, menu, previousPage: previousPage )
		{
			ToStringConverter = toStringConverter ?? ( x => x.ToString ( ) );
			ExtractLabels ( rows );

			foreach ( var label in selectionMap.Keys )
				AddHandler ( label, QueryHandlerAsync );

			AddWideLabel ( "Back" );
			AddHandler ( "Back", async q => UserChoseBack = true );

			BuildSpecialKeyboard ( );
		}

		private void BuildSpecialKeyboard ( )
		{
			var keyboard = new List<List<InlineKeyboardButton>> ( );

			foreach ( var row in Labels )
			{
				var buttonRow = new List<InlineKeyboardButton> ( );
				foreach ( var label in row )
				{
					var button = new InlineKeyboardButton
					{
						CallbackData = label,
						Text = selectionMap.TryGetValue ( label, out var item )
							? ToStringConverter ( item )
							: label
					};
					buttonRow.Add ( button );
				}

				keyboard.Add ( buttonRow );
			}

			Keyboard = new InlineKeyboardMarkup ( keyboard );
		}

		private void ExtractLabels ( IEnumerable<IEnumerable<T>> rows )
		{
			var labels = new List<List<string>> ( );

			foreach ( var row in rows )
			{
				var stringRow = new List<string> ( );
				foreach ( var item in row )
				{
					var label = item.ToString ( );
					stringRow.Add ( label );
					selectionMap[label] = item;
				}

				labels.Add ( stringRow );
			}

			Labels = labels;
		}

		private async Task QueryHandlerAsync ( CallbackQuery query )
		{
			var label = query.Data;

			if ( selectionMap.TryGetValue ( label, out var result ) )
				Result = result;
		}

		public async Task<SelectionResult> DisplayAndWaitAsync ( )
		{
			await Menu.SwitchPageAsync ( this ).ConfigureAwait ( false );
			await WaitForButtonPressAsync ( ).ConfigureAwait ( false );

			return new SelectionResult ( !UserChoseBack, Result );
		}

		public struct SelectionResult
		{
			public SelectionResult ( bool hasResult,
			                         T result )
			{
				HasResult = hasResult;
				Result    = result;
			}

			public bool HasResult { get; }
			public T Result { get; }

			public static implicit operator bool ( SelectionResult selection ) => selection.HasResult;
			public static implicit operator T ( SelectionResult selection ) => selection.Result;
		}
	}
}