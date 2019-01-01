[![Build status](https://ci.appveyor.com/api/projects/status/lme9yn9rx8642i1l/branch/master?svg=true)](https://ci.appveyor.com/project/DevilDaga/cryptotickerbot/branch/master) [![Build Status](https://travis-ci.com/HarshDaga/CryptoTickerBot.svg?branch=master)](https://travis-ci.com/HarshDaga/CryptoTickerBot)

# Crypto Ticker Bot

A simple bot built in .NET Core to fetch cryptocurrency prices from various [exchanges](#supported-exchanges) using ticker data.
The crypto data can be relayed to destinations like Telegram and Google Sheets if desired.

This is **NOT** an auto-trader. Instead, this bot is to simply notify you of significant price changes on any coin from the list of supported exchanges given your subscription preference.

## Getting Started

One instance of the bot is run 24x7 by me and the corresponding Telegram bot for the same can be found [here.](https://t.me/CryptoExchangeTickerBot)

You can also create your own instance by compiling the source and creating the appropriate config files before running [CryptoTickerBot.Runner](CryptoTickerBot.Runner).

### Prerequisites

  - .NET Core 2.2 SDK
  - API Key from [Fixer](https://fixer.io/product). The free one will do
  - [Google Sheets API](https://console.developers.google.com/apis/library/sheets.googleapis.com) if Google Sheets target is desired
  - [Telegram Bot Token](https://telegram.me/botfather) if Telegram target is desired

## Supported Exchanges

  - [Binance](https://www.binance.com/)
  - [Coinbase](https://www.coinbase.com/)
  - [CoinDelta](https://coindelta.com/)
  - [Koinex](https://koinex.in/)
  - [Kraken](https://www.kraken.com/)
  - [Bitstamp](https://www.bitstamp.net/)

## Features

  - ### Price Change Alert
      A subscription that gets invoked for every **N** percent change in the last traded price of a coin.
  - ### [Triangular Arbitrage](https://en.wikipedia.org/wiki/Triangular_arbitrage)
      For all active exchanges, all possible triangular arbitrage opportunities are discovered and reported as soon as the coin prices get updated.
  - ### Console Target
      If enabled, all incoming coin values are displayed on the console.
  - ### [Telegram Bot](https://telegram.org/blog/bot-revolution) Integration
      Price change alerts and a keyboard menu in [Telegram](https://telegram.org/) that can be accessed in personal as well as group chats. Subscription alerts can be set to silent in preferences.
      
      ![Subscription creation](https://media.giphy.com/media/AFggFCTxuV1mNq1ShZ/giphy.gif)
  - ### [Google Sheets](https://www.google.com/sheets/about/) Integration
      Fundamental data of all coins in all active exchanges is sorted and stored into a Google spreadsheet every given interval.

## Running the tests

[CryptoTickerBot.UnitTests](CryptoTickerBot.UnitTests) defines some very basic tests for the core data structures and algorithms used in this project

The tests can be run using [NUnit](https://www.nuget.org/packages/NUnit/).

## Deployment

1. Run [CryptoTickerBot.Runner](CryptoTickerBot.Runner) once.
   The program will log an error and terminate. Don't panic, this was expected.

2. A new folder called **Configs** is now created in the same directory as the executable.
   In this folder are the configuration files which need to be filled in either manually or by assigning default values to the config properties in the solution and rebuilding. However, it's recommended not to use API keys anywhere in the code.
   The configs can be filled in manually by following these steps:

   1. Open **Configs\Core.json** and add this entry
      ```json
      "FixerApiKey": "<your key here>"
      ```
      Everything else that you see in this file was created by default and can be edited manually when needed.

   2. Open **Configs\Runner.json** and choose the services you wish to enable

   3. If Google Sheets service was enabled, open **Configs\Sheets.json** and fill in these entries

      ``` json
      "SpreadSheetId": "<your spreadsheet id from Google>",
      "SheetName": "<your spreadsheet name>",
      "SheetId": int, // spreadsheet number defaults to 0
      "ApplicationName": "<your application name from Google>"
      ```

   4. If Telegram service was enabled, open **Configs\TelegramBot.json** and add this entry

      ```json
      "BotToken": "<your telegram bot token>"
      ```

## Built With

Here is a list of all the [dependencies](https://github.com/HarshDaga/CryptoTickerBot/network/dependencies) of this project.

## Contributing

Please read [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) for details on our code of conduct, and the process for submitting pull requests to us.

## Authors

* **Harsh Daga** - *Initial work*
* **Daniel Dudziński** - *Helping with deployment and CI*

See also the list of [contributors](https://github.com/HarshDaga/CryptoTickerBot/graphs/contributors) who participated in this project.

## License

This project is licensed under the GNU General Public License - see the [LICENSE](LICENSE) file for details.
