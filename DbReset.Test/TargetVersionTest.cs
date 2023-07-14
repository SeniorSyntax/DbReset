using NUnit.Framework;
using SharpTestsEx;

namespace DbReset.Test;

public class TargetVersionTest
{
	private const string connectionString = TestConnectionString.ConnectionString;

	[SetUp]
	public void Setup() =>
		connectionString.DropTestDatabase();

	[Test]
	public void ShouldRestoreFromBackupOfTargetVersion()
	{
		connectionString.CreateTestDatabase();

		connectionString.Execute("CREATE TABLE t1 (c1 int null)");
		var cacheOptions = new CacheOptions
		{
			ConnectionString = connectionString,
			Key = $"DbReset.Test.{TestContext.CurrentContext.Test.Name}",
			Version = "1"
		};
		DatabaseCache.Store(cacheOptions);

		var result = DatabaseCache.TryReset(cacheOptions);

		result.Should().Be.True();
		var tables = connectionString.Query<string>("SELECT TABLE_NAME FROM information_schema.tables");
		tables.Should().Contain("t1");
		tables.Should().Not.Contain("t2");
	}
}
