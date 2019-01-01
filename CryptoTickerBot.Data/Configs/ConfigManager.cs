using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NLog;

// ReSharper disable InconsistentlySynchronizedField

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

		public static ImmutableHashSet<Type> InitializedConfigs { get; private set; } = ImmutableHashSet<Type>.Empty;

		public static ImmutableHashSet<Type> SetInitialized<TConfig> ( )
			where TConfig : IConfig<TConfig> =>
			InitializedConfigs = InitializedConfigs.Add ( typeof ( TConfig ) );
	}

	public static class ConfigManager<TConfig> where TConfig : IConfig<TConfig>, new ( )
	{
		[UsedImplicitly] private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private static readonly object FileLock;
		private static TConfig instance;

		public static ref TConfig Instance => ref instance;

		public static Exception LastError { get; private set; }

		public static string FileName =>
			Path.Combine ( instance.ConfigFolderName ?? "Configs", $"{instance.ConfigFileName}.json" );

		public static string Serialized =>
			JsonConvert.SerializeObject ( instance, ConfigManager.SerializerSettings );

		static ConfigManager ( )
		{
			FileLock = new object ( );
			instance = new TConfig ( );
			Load ( );
			Save ( );

			ConfigManager.SetInitialized<TConfig> ( );
		}

		public static void ClearLastError ( ) =>
			LastError = null;

		public static bool TryValidate ( out IList<Exception> exceptions ) =>
			instance.TryValidate ( out exceptions );

		public static void Save ( )
		{
			try
			{
				lock ( FileLock )
				{
					if ( !Directory.Exists ( instance.ConfigFolderName ) )
						Directory.CreateDirectory ( instance.ConfigFolderName );
					File.WriteAllText ( FileName, Serialized );
				}
			}
			catch ( Exception e )
			{
				LastError = e;
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
						instance = JsonConvert.DeserializeObject<TConfig> ( File.ReadAllText ( FileName ),
						                                                    ConfigManager.SerializerSettings );
				}
			}
			catch ( Exception e )
			{
				LastError = e;
				Logger.Error ( e );
			}
		}

		public static void RestoreDefaults ( )
		{
			try
			{
				instance = instance.RestoreDefaults ( );
				lock ( FileLock )
				{
					if ( !Directory.Exists ( instance.ConfigFolderName ) )
						Directory.CreateDirectory ( instance.ConfigFolderName );
					File.WriteAllText ( FileName, Serialized );
				}
			}
			catch ( Exception e )
			{
				LastError = e;
				Logger.Error ( e );
			}
		}

		public static void Reset ( )
		{
			try
			{
				lock ( FileLock )
				{
					if ( File.Exists ( FileName ) )
						File.Delete ( FileName );

					instance = new TConfig ( );

					if ( !Directory.Exists ( instance.ConfigFolderName ) )
						Directory.CreateDirectory ( instance.ConfigFolderName );
					File.WriteAllText ( FileName, Serialized );
				}
			}
			catch ( Exception e )
			{
				LastError = e;
				Logger.Error ( e );
			}
		}
	}
}