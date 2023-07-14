using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SharpTestsEx;

namespace DbReset.Test;

[ApplicableToSqlServer]
public class InvalidationFileTest
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
		var backupFileName = (from p in prefixes
							  from f in Directory.GetFiles(@"c:\temp\dbcache\", $"{p}*.dbcache")
							  select f).Single();
		var backupFile = new FileInfo(backupFileName);
		var backup = JsonConvert.DeserializeObject<Backup>(File.ReadAllText(backupFile.FullName));
		var backupFiles = backup.Files.Select(x => x.Backup);
		var filesToInvalidate = backupFiles.Append(backupFile.FullName).ToArray();
		filesToInvalidate.Should().Have.Count.GreaterThan(0);

		connectionString.Execute("CREATE TABLE T2 (C1 int null)");
		cacheOptions.Version = "2";
		DatabaseCache.Store(cacheOptions);

		filesToInvalidate.Select(File.Exists).Should().Not.Contain(true);
	}

	internal class Backup
	{
		public IEnumerable<BackupFile> Files { get; set; }
	}

	internal class BackupFile
	{
		public string Source { get; set; }
		public string Backup { get; set; }
		public string Destination { get; set; }
	}
}
