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

		public static T Get<T> ( ) where T : IConfig<T>, new ( ) =>
			ConfigManager<T>.Instance;
	}

	public static class ConfigManager<TConfig> where TConfig : IConfig<TConfig>, new ( )
	{
		[UsedImplicitly] private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private static readonly object FileLock;
		public static TConfig Instance { get; set; }

		public static string FileName =>
			Path.Combine ( Instance.ConfigFolderName ?? "Configs", $"{Instance.ConfigFileName}.json" );

		static ConfigManager ( )
		{
			FileLock = new object ( );
			Instance = new TConfig ( );
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
						Instance = JsonConvert.DeserializeObject<TConfig> ( File.ReadAllText ( FileName ),
						                                                    ConfigManager.SerializerSettings );
				}
			}
			catch ( Exception e )
			{
				Logger.Error ( e );
			}
		}

		public static void RestoreDefaults ( )
		{
			try
			{
				Instance = Instance.RestoreDefaults ( );
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

		public static void Reset ( )
		{
			try
			{
				lock ( FileLock )
				{
					if ( !File.Exists ( FileName ) )
						return;

					File.Delete ( FileName );
					Instance = new TConfig ( );

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