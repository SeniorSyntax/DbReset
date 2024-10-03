using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using DbAgnostic;
using NUnit.Framework;
using SharpTestsEx;

namespace DbReset.Test;

[ApplicableToPostgres]
public class LengthyInputsDatabaseTemplateTest
{
	private const string connectionString = TestConnectionString.ConnectionString;

	[SetUp]
	public void Setup()
	{
		connectionString.RecreateTestDatabase();
	}

	[Test]
	public void ShouldBackupLongInputsProperly()
	{
		connectionString.Execute("CREATE TABLE T (C1 int null)");
		var key = "AVeryVeryLongString_Hb7bdrPeh3UYtq4ruhuQWLLctmU9HJ1pnM6JoG2Qx37dAriQNCPSzBPCgnWyVP0VQkk6wPpLe2VwFozHu2CVw9yr3djjVXiU9gzZf8ZK6Jl8";
		var contentVersion = "AVeryVeryLongString_v1WtGJTXnDGl0bXE1pIhV3ChwLzhIABox3zu6IVDG6AmpvaPVjsaiFRbUUFA1ui5d4E6PgJFipwAzJ79XCj1huONRFBpjg3P7cFLuGL8Lamj";
		var cacheOptions = new CacheOptions
		{
			ConnectionString = connectionString,
			Key = key,
			Version = $"1.{contentVersion}",
			Output = new ConsoleOutput()
		};

		DatabaseCache.Store(cacheOptions);

		var prefixes = BackupNameBuilder.PossibleKeysForKey(key);
		var databaseNames = connectionString.PointToMasterDatabase().Query<string>($"SELECT datname FROM pg_catalog.pg_database");
		var databaseBackups = (
			from db in databaseNames
			from p in prefixes
			where db.EndsWith(BackupNameBuilder.Extension())
			where db.StartsWith(p)
			select db
		).ToArray();
		databaseBackups.Should().Have.Count.EqualTo(1);
	}

	[Test]
	public void ShouldResetDatabaseInArabicCulture()
	{
		var cacheOptions = new CacheOptions
		{
			ConnectionString = connectionString,
			Key = "AKeyThatWillResultInNegativeHash_PihGkqSFkVopFtyqGcYJxjfRYHcSj4QGlctKYaHXUyjbdWMhpVrOicLFdARMnawqeSeCaXBFEaZaadOc45CyMcyiBXHxs4K",
			Version = "1234.1890764580.1714",
			Output = new ConsoleOutput()
		};
		DatabaseCache.Store(cacheOptions);

		Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
		var result = DatabaseCache.TryReset(cacheOptions);

		result.Should().Be.True();
	}

	[Test]
	public void ShouldResetDatabaseInSwedishCulture()
	{
		var cacheOptions = new CacheOptions
		{
			ConnectionString = connectionString,
			Key = "AKeyThatWillResultInNegativeHash_kXPNbP0lZdALArLT6rxiCwmENW0r0UXbBYDdU2fQeRztEyPS9edguwLHX31OLDVmpU1o6NPqaDU8i9qUo3cJUXG73HIYFRK",
			Version = "1234.1890764580.1714",
			Output = new ConsoleOutput()
		};
		DatabaseCache.Store(cacheOptions);

		Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");
		var result = DatabaseCache.TryReset(cacheOptions);

		result.Should().Be.True();
	}

	[Test]
	[Explicit]
	public void LongStringGenerator()
	{
		var key = randomLongString("AVeryVeryLongString_", 128);
		var hash = getDeterministicHashCode(key);
		Console.WriteLine("This string sample is very long!");
		Console.WriteLine("String: " + key);
		Console.WriteLine("Hash: " + hash);
	}

	[Test]
	[Explicit]
	public void NegativeHashGenerator()
	{
		var key = randomLongString("AKeyThatWillResultInNegativeHash_", 128);
		var hash = getDeterministicHashCode(key);
		while (hash > 0)
		{
			key = randomLongString("AKeyThatWillResultInNegativeHash_", 128);
			hash = getDeterministicHashCode(key);
		}

		Console.WriteLine("This string sample should return in negative hash!");
		Console.WriteLine("String: " + key);
		Console.WriteLine("Hash: " + hash);
	}

	private static string randomLongString(string prefix, int length)
	{
		const string chars = "abcdefghijklmnopqrstuvwyxzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		var builder = new StringBuilder();
		builder.Append(prefix);
		var random = new Random();

		while (builder.Length < length)
		{
			var c = chars[random.Next(0, chars.Length)];
			builder.Append(c);
		}

		return builder.ToString();
	}

	private static int getDeterministicHashCode(string str)
	{
		unchecked
		{
			var hash1 = (5381 << 16) + 5381;
			var hash2 = hash1;

			for (int i = 0; i < str.Length; i += 2)
			{
				hash1 = ((hash1 << 5) + hash1) ^ str[i];
				if (i == str.Length - 1)
					break;
				hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
			}

			return hash1 + (hash2 * 1566083941);
		}
	}
}