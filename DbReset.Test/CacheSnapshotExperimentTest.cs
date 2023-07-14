using DbAgnostic;
using NUnit.Framework;
using SharpTestsEx;

namespace DbReset.Test;

[ApplicableToSqlServer]
public class CacheSnapshotExperimentTest
{
	private const string connectionString = TestConnectionString.ConnectionString;
	private string connectionStringNew;

	[SetUp]
	public void Setup()
	{
		connectionStringNew = connectionString.ChangeDatabase("DbReset.Test.New");
		connectionString.RecreateTestDatabase();
		connectionStringNew.RecreateTestDatabase();
	}

	[Test]
	public void ShouldNotRestoreWhenNoBackup()
	{
		connectionString.Execute("CREATE TABLE T (C1 int null)");
		connectionString.Execute("INSERT INTO T VALUES(123)");

		var result = DatabaseCache.TryReset(new CacheOptions
		{
			ConnectionString = connectionString,
			Key = $"DbReset.Test.{TestContext.CurrentContext.Test.Name}",
			Version = "1",
			Strategy = new SqlServerSnapshotStrategy_Experimental()
		});

		result.Should().Be.False();
		connectionString.ExecuteScalar<int>("SELECT COUNT(*) FROM T")
			.Should().Be(1);
	}

	[Test]
	public void ShouldNotRestoreWhenNoDatabaseOrBackup()
	{
		connectionString.DropTestDatabase();

		var result = DatabaseCache.TryReset(new CacheOptions
		{
			ConnectionString = connectionString,
			Key = $"DbReset.Test.{TestContext.CurrentContext.Test.Name}",
			Version = "1",
			Strategy = new SqlServerSnapshotStrategy_Experimental()
		});

		result.Should().Be.False();
	}

	[Test]
	public void ShouldRestoreFromBackup()
	{
		connectionString.Execute("CREATE TABLE T (C1 int null)");
		connectionString.Execute("INSERT INTO T VALUES(1)");
		var cacheOptions = new CacheOptions
		{
			ConnectionString = connectionString,
			Key = $"DbReset.Test.{TestContext.CurrentContext.Test.Name}",
			Version = "1",
			Strategy = new SqlServerSnapshotStrategy_Experimental()
		};
		DatabaseCache.Store(cacheOptions);

		connectionString.Execute("INSERT INTO T VALUES(2)");
		var result = DatabaseCache.TryReset(cacheOptions);

		result.Should().Be.True();
		connectionString.ExecuteScalar<int>("SELECT COUNT(*) FROM T")
			.Should().Be(1);
	}

	[Test]
	[Ignore("Snapshot doesnt work this way I think")]
	public void ShouldRestoreToNewFromBackup()
	{
		connectionString.Execute("CREATE TABLE T (C1 int null)");
		connectionString.Execute("INSERT INTO T VALUES(1)");
		var cacheOptions = new CacheOptions
		{
			ConnectionString = connectionString,
			Key = $"DbReset.Test.{TestContext.CurrentContext.Test.Name}",
			Version = "1",
			Strategy = new SqlServerSnapshotStrategy_Experimental()
		};
		DatabaseCache.Store(cacheOptions);

		cacheOptions.ConnectionString = connectionStringNew;

		var result = DatabaseCache.TryReset(cacheOptions);

		result.Should().Be.True();
		connectionStringNew.ExecuteScalar<int>("SELECT COUNT(*) FROM T")
			.Should().Be(1);
	}
}
