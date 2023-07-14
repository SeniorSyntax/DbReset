using System;
using System.Linq;
using System.Text;
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
		var key = longString($"DbReset.Test.{TestContext.CurrentContext.Test.Name}", 128);
		var contentVersion = longString("contentVersion", 128);
		var cacheOptions = new CacheOptions
		{
			ConnectionString = connectionString,
			Key = key,
			Version = $"1.{contentVersion}"
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

	private static string longString(string prefix, int length)
	{
		const string chars = "abcdefghijklmnopqrstuvwyxzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		var builder = new StringBuilder();
		builder.Append(prefix);
		var random = new Random(42);

		while (builder.Length < length)
		{
			var c = chars[random.Next(0, chars.Length)];
			builder.Append(c);
		}

		return builder.ToString();
	}
}
