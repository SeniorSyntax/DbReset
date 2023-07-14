using System.Linq;
using DbAgnostic;
using NUnit.Framework;
using SharpTestsEx;

namespace DbReset.Test;

[ApplicableToPostgres]
public class InvalidationDatabaseTemplateTest
{
	private const string connectionString = TestConnectionString.ConnectionString;

	[SetUp]
	public void Setup()
	{
		connectionString.RecreateTestDatabase();
	}

	[Test]
	public void ShouldRemoveOldBackups()
	{
		connectionString.Execute("CREATE TABLE T1 (C1 int null)");
		var cacheOptions = new CacheOptions
		{
			ConnectionString = connectionString,
			Key = $"DbReset.Test.{TestContext.CurrentContext.Test.Name}",
			Version = "1"
		};
		DatabaseCache.Store(cacheOptions);

		var prefixes = BackupNameBuilder.PossibleKeysForKey($"DbReset.Test.{TestContext.CurrentContext.Test.Name}");
		var databaseNames = connectionString.PointToMasterDatabase().Query<string>($"SELECT datname FROM pg_catalog.pg_database");
		var databaseToInvalidate = (
			from db in databaseNames
			from p in prefixes
			where db.EndsWith(BackupNameBuilder.Extension())
			where db.StartsWith(p)
			select db
		).Single();
		connectionString.PointToMasterDatabase().Query<string>($"SELECT datname FROM pg_catalog.pg_database")
			.Should().Contain(databaseToInvalidate);

		connectionString.Execute("CREATE TABLE T2 (C1 int null)");
		cacheOptions.Version = "2";
		DatabaseCache.Store(cacheOptions);

		connectionString.PointToMasterDatabase().Query<string>($"SELECT datname FROM pg_catalog.pg_database")
			.Should().Not.Contain(databaseToInvalidate);
	}
}
