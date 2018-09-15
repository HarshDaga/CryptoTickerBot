using Newtonsoft.Json;

namespace CryptoTickerBot.Data.Domain
{
	public class TelegramBotUser
	{
		[JsonProperty ( Required = Required.Always )]
		public int Id { get; set; }

		[JsonProperty ( Required = Required.Always )]
		public bool IsBot { get; set; }

		[JsonProperty ( Required = Required.Always )]
		public string FirstName { get; set; }

		[JsonProperty ( DefaultValueHandling = DefaultValueHandling.Ignore )]
		public string LastName { get; set; }

		[JsonProperty ( DefaultValueHandling = DefaultValueHandling.Ignore )]
		public string Username { get; set; }

		[JsonProperty ( DefaultValueHandling = DefaultValueHandling.Ignore )]
		public string LanguageCode { get; set; }

		[JsonProperty ( Required = Required.Always )]
		public UserRole Role { get; set; }
	}
}