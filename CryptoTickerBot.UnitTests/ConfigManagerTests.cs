using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CryptoTickerBot.Data.Configs;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CryptoTickerBot.UnitTests
{
	using MockConfigManager = ConfigManager<ConfigManagerTests.MockConfig>;

	[TestFixture]
	public class ConfigManagerTests
	{
		[SetUp]
		public void Setup ( )
		{
			if ( Directory.Exists ( MockConfigFolderName ) )
				Directory.Delete ( MockConfigFolderName, true );
		}

		[TearDown]
		public void TearDown ( )
		{
			MockConfigManager.Reset ( );
		}

		public const string MockConfigFileName = "Mock";
		public const string MockConfigFolderName = "Configs";

		public static string ConfigPath =>
			$"{Path.Combine ( MockConfigFolderName, MockConfigFileName )}.json";

		public class MockConfig : IConfig<MockConfig>
		{
			public string ConfigFileName { get; } = MockConfigFileName;
			public string ConfigFolderName { get; } = MockConfigFolderName;
			public int IntValueWithDefault { get; set; } = 42;
			public string StringValueWithDefault { get; set; } = "Foo";
			public List<string> ListWithDefaultValues { get; set; } = new List<string> {"One", "Two", "Three"};
			public int SomeSecretKey { get; set; }

			public bool Validate ( out IList<Exception> exceptions )
			{
				exceptions = new List<Exception> ( );

				if ( SomeSecretKey == 0 )
					exceptions.Add ( new ArgumentException ( "Secret Key not set", nameof ( SomeSecretKey ) ) );

				return !exceptions.Any ( );
			}

			public MockConfig RestoreDefaults ( ) =>
				new MockConfig {SomeSecretKey = SomeSecretKey};
		}

		private static string Serialize<T> ( T obj ) =>
			JsonConvert.SerializeObject ( obj, ConfigManager.SerializerSettings );

		[Test]
		public void ConfigFileInUseShouldSetAppropriateError ( )
		{
			MockConfigManager.ClearLastError ( );
			Assert.IsNull ( MockConfigManager.LastError );
			Directory.CreateDirectory ( MockConfigFolderName );
			using ( File.Create ( ConfigPath ) )
			{
				Assert.DoesNotThrow ( MockConfigManager.Load );
				Assert.IsInstanceOf<IOException> ( MockConfigManager.LastError );
				MockConfigManager.ClearLastError ( );

				Assert.DoesNotThrow ( MockConfigManager.Save );
				Assert.IsInstanceOf<IOException> ( MockConfigManager.LastError );
				MockConfigManager.ClearLastError ( );
			}
		}

		[Test]
		public void ConfigShouldHaveDefaultValuesOnCreation ( )
		{
			var config = MockConfigManager.Instance;
			Assert.AreEqual ( config.IntValueWithDefault, 42 );
			Assert.AreEqual ( config.StringValueWithDefault, "Foo" );
			Assert.True ( config.ListWithDefaultValues.SequenceEqual ( new[] {"One", "Two", "Three"} ) );
			Assert.AreEqual ( config.SomeSecretKey, default ( int ) );
		}

		[Test]
		public void ConfigInstanceShouldReturnByRef ( )
		{
			ref var config1 = ref MockConfigManager.Instance;

			Assert.AreEqual ( config1.SomeSecretKey, default ( int ) );
			config1.SomeSecretKey = 0xDEAD;
			Assert.AreEqual ( config1.SomeSecretKey, 0xDEAD );

			MockConfigManager.Save ( );
			MockConfigManager.Load ( );

			ref var config2 = ref MockConfigManager.Instance;
			Assert.AreEqual ( config2.SomeSecretKey, 0xDEAD );

			Assert.AreSame ( config1, config2 );
		}

		[Test]
		public void ConfigLoadShouldOverwriteDataFromDisk ( )
		{
			ref var config = ref MockConfigManager.Instance;

			Assert.AreEqual ( config.IntValueWithDefault, 42 );
			Assert.AreEqual ( config.SomeSecretKey, default ( int ) );

			var other = new MockConfig
			{
				IntValueWithDefault = 10,
				SomeSecretKey       = 0xDEAD
			};
			Directory.CreateDirectory ( MockConfigFolderName );
			File.WriteAllText ( ConfigPath, Serialize ( other ) );

			MockConfigManager.Load ( );
			Assert.AreEqual ( config.IntValueWithDefault, 10 );
			Assert.AreEqual ( config.SomeSecretKey, 0xDEAD );
		}

		[Test]
		public void ConfigManagerShouldRespectSerializerSettings ( )
		{
			var settings = new JsonSerializerSettings
			{
				Formatting        = Formatting.None,
				NullValueHandling = NullValueHandling.Include
			};

			var original = ConfigManager.SerializerSettings;

			ConfigManager.SerializerSettings = settings;
			var config = MockConfigManager.Instance;
			MockConfigManager.Save ( );

			Assert.AreEqual ( JsonConvert.SerializeObject ( config, settings ),
			                  MockConfigManager.Serialized );

			ConfigManager.SerializerSettings = original;
		}

		[Test]
		public void ConfigResetShouldOverwriteEverything ( )
		{
			ref var config = ref MockConfigManager.Instance;

			Assert.AreEqual ( config.IntValueWithDefault, 42 );
			Assert.AreEqual ( config.SomeSecretKey, default ( int ) );

			config.SomeSecretKey       = 0xDEAD;
			config.IntValueWithDefault = 10;
			Assert.AreEqual ( config.IntValueWithDefault, 10 );
			Assert.AreEqual ( config.SomeSecretKey, 0xDEAD );

			MockConfigManager.Reset ( );
			Assert.AreEqual ( config.IntValueWithDefault, 42 );
			Assert.AreEqual ( config.SomeSecretKey, default ( int ) );
		}

		[Test]
		public void ConfigRestoreDefaultsShouldOverwriteOnlyChosenMembers ( )
		{
			ref var config = ref MockConfigManager.Instance;

			Assert.AreEqual ( config.IntValueWithDefault, 42 );
			Assert.AreEqual ( config.SomeSecretKey, default ( int ) );

			config.SomeSecretKey       = 0xDEAD;
			config.IntValueWithDefault = 10;
			Assert.AreEqual ( config.IntValueWithDefault, 10 );
			Assert.AreEqual ( config.SomeSecretKey, 0xDEAD );

			MockConfigManager.RestoreDefaults ( );
			Assert.AreEqual ( config.IntValueWithDefault, 42 );
			Assert.AreEqual ( config.SomeSecretKey, 0xDEAD );
		}

		[Test]
		public void ConfigSaveShouldWriteDataToDisk ( )
		{
			var config = MockConfigManager.Instance;

			config.SomeSecretKey = 0xDEAD;
			Assert.AreEqual ( config.SomeSecretKey, 0xDEAD );

			MockConfigManager.Save ( );
			Assert.AreEqual ( Serialize ( config ), File.ReadAllText ( ConfigPath ) );
		}

		[Test]
		public void ConfigValidateTest ( )
		{
			var config = MockConfigManager.Instance;
			Assert.AreEqual ( config.SomeSecretKey, 0 );
			Assert.False ( config.Validate ( out var exceptions ) );
			Assert.AreEqual ( exceptions.Count, 1 );
			var ae = exceptions[0] as ArgumentException;
			Assert.NotNull ( ae );
			Assert.AreEqual ( ae.ParamName, nameof ( MockConfig.SomeSecretKey ) );
		}

		[Test]
		[Order ( 1 )]
		public void GetConfigInstanceShouldCreateCorrectFileOnDisk ( )
		{
			Warn.If ( ConfigManager.InitializedConfigs.Contains ( typeof ( MockConfig ) ) );

			var _ = MockConfigManager.Instance;
			Assert.AreEqual ( MockConfigManager.FileName, ConfigPath );
			Warn.Unless ( File.Exists ( ConfigPath ) );

			Warn.Unless ( ConfigManager.InitializedConfigs.Contains ( typeof ( MockConfig ) ) );
		}
	}
}