using System.Collections.Generic;
using System.Linq;
using DbAgnostic;
using NUnit.Framework;

namespace DbReset.Test;

public class ConcurrencyTest
{
	private const string connectionString = TestConnectionString.ConnectionString;

	private readonly IEnumerable<string> connectionStrings = Enumerable.Range(0, 10)
		.Select(x => connectionString.ChangeDatabase(connectionString.DatabaseName() + "_" + x))
		.ToArray();

	[SetUp]
	public void Setup() =>
		connectionStrings.ForEach(c => c.DropTestDatabase());

	[TearDown]
	public void TearDown() =>
		connectionStrings.ForEach(c => c.DropTestDatabase());

	[Test]
	public void ShouldWork()
	{
		var locker = new object();
		var runner = new ConcurrencyRunner();

		connectionStrings.ForEach(c =>
		{
			var cacheOptions = new CacheOptions
			{
				ConnectionString = c,
				Key = $"DbReset.Test.{TestContext.CurrentContext.Test.Name}"
			};

			runner.InParallel(() =>
			{
				if (DatabaseCache.TryReset(cacheOptions))
					return;
				lock (locker)
				{
					if (DatabaseCache.TryReset(cacheOptions))
						return;
					c.RecreateTestDatabase();
					DatabaseCache.Store(cacheOptions);
				}
			});
		});

		runner.Wait();
	}
}
