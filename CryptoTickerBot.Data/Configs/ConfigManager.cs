using System;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NLog;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

// ReSharper disable StaticMemberInGenericType

namespace CryptoTickerBot.Data.Configs
{
	public static class ConfigManager
	{
		public static JsonSerializerSettings SerializerSettings { get; set; } = new JsonSerializerSettings
		{
			Formatting             = Formatting.Indented,
			NullValueHandling      = NullValueHandling.Ignore,
			ObjectCreationHandling = ObjectCreationHandling.Replace
		};

		public static T Get<T> ( ) where T : IConfig, new ( ) =>
			ConfigManager<T>.Instance;
	}

	public static class ConfigManager<T> where T : IConfig, new ( )
	{
		[UsedImplicitly] private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private static readonly object FileLock;
		public static T Instance { get; set; }

		public static string FileName => Path.Combine ( "Configs", $"{Instance.ConfigFileName}.json" );

		static ConfigManager ( )
		{
			FileLock = new object ( );
			Instance = new T ( );
			Load ( );
			Save ( );
		}

		public static void Save ( )
		{
			try
			{
				lock ( FileLock )
				{
					File.WriteAllText ( FileName,
					                    JsonConvert.SerializeObject ( Instance, ConfigManager.SerializerSettings ) );
				}
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		public static void Load ( )
		{
			try
			{
				lock ( FileLock )
				{
					if ( File.Exists ( FileName ) )
						Instance = JsonConvert.DeserializeObject<T> ( File.ReadAllText ( FileName ),
						                                              ConfigManager.SerializerSettings );
				}
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		public static void Reset ( )
		{
			try
			{
				lock ( FileLock )
				{
					if ( !File.Exists ( FileName ) )
						return;

					File.Delete ( FileName );
					Instance = new T ( );

					File.WriteAllText ( FileName,
					                    JsonConvert.SerializeObject ( Instance, ConfigManager.SerializerSettings ) );
				}
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}
	}
}