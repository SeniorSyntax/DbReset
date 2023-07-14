using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace DbReset.Test;

public class PerformanceTest
{
	private const string connectionString = TestConnectionString.ConnectionString;

	[SetUp]
	public void Setup()
	{
		connectionString.RecreateTestDatabase();
	}

	[Test]
	[Explicit]
	public void ShouldSwooosh()
	{
		var iterations = 100;
		var tableCount = 500;
		var columnCount = 100;
		var rowCount = 3;
		var columns = string.Join(", ",
			Enumerable.Range(0, columnCount)
				.Select(i => $@"C{i} int null")
				.ToArray()
		);
		var tables = Enumerable.Range(0, tableCount)
			.Select(i => $@"T{i}")
			.ToArray();
		tables.ForEach(table =>
		{
			connectionString.Execute($"CREATE TABLE {table} ({columns})");
		});
		var values = string.Join(", ",
			Enumerable.Range(0, columnCount)
				.Select(i => "1")
				.ToArray()
		);
		tables.ForEach(table =>
		{
			connectionString.Execute($"INSERT INTO {table} VALUES ({values})");
		});
		var cacheOptions = new CacheOptions
		{
			ConnectionString = connectionString,
			Key = $"DbReset.Test.{TestContext.CurrentContext.Test.Name}",
			Version = "1"
		};
		DatabaseCache.Store(cacheOptions);

		var timer = new Stopwatch();
		timer.Start();
		Enumerable.Range(0, iterations)
			.ForEach(x =>
			{
				DatabaseCache.TryReset(cacheOptions);
			});
		timer.Stop();

		var average = timer.Elapsed.TotalSeconds / iterations;
		Console.WriteLine($"Restored database {iterations} times in {timer.Elapsed.TotalSeconds} seconds.");
		Console.WriteLine($"Average time {average}");
		Console.WriteLine($"{tableCount} tables with {columnCount} columns and {rowCount} rows each.");
	}
}
