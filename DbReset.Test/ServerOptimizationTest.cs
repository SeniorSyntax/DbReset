using System;
using System.Linq;
using NUnit.Framework;
using SharpTestsEx;

namespace DbReset.Test;

[ApplicableToPostgres]
public class ServerOptimizationTest
{
	private const string connectionString = TestConnectionString.ConnectionString;

	[SetUp]
	public void Setup()
	{
		connectionString.RecreateTestDatabase();
	}
	
	// postgres optimization
	// https://stackoverflow.com/questions/9407442/optimise-postgresql-for-fast-testing
	[Test]
	public void ShouldApplyPostgresServerSettings()
	{
		connectionString.Execute("ALTER SYSTEM RESET ALL");
		connectionString.Execute("select pg_reload_conf();");
		
		var cacheOptions = new CacheOptions
		{
			ConnectionString = connectionString,
			Key = $"DbReset.Test.{TestContext.CurrentContext.Test.Name}",
			Version = "1",
			OptimizePostgreSqlForFastTesting = true
		};
		DatabaseCache.Store(cacheOptions);
		
		var values = connectionString
			.Query<(string name, string setting)>("select * from pg_settings where name in ('fsync', 'full_page_writes', 'shared_buffers', 'work_mem');")
			.ToDictionary(x => x.name, x => x.setting);
		values["fsync"].Should().Be("off");
		values["full_page_writes"].Should().Be("off");
		values["shared_buffers"].Should().Be("163840");
		values["work_mem"].Should().Be("512000");
	}
}