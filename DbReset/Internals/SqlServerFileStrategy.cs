using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DbReset.Internals;

internal class SqlServerFileStrategy : ICacheStrategy
{
	public void Backup(ICacheContext context)
	{
		sqlServerFileCopyBackup(context, BackupName(context));
	}

	public void Invalidate(ICacheContext context, IEnumerable<string> prefixes)
	{
		sqlServerFileCopyInvalidation(context, prefixes);
	}

	public string BackupName(ICacheContext context) =>
		BackupNameBuilder.GetNameWithMaxLength(context.Key(), context.Version(), int.MaxValue);

	private static void sqlServerFileCopyBackup(ICacheContext context, string backupName)
	{
		var databaseName = context.DatabaseName();

		var filesNames = context.DatabaseConnector().Query<string>("select filename from sys.sysfiles");

		var backup = new Backup
		{
			Files = (from source in filesNames
					let destination = source.Replace(databaseName, "$(DatabaseName)")
					select new BackupFile
					{
						Source = source,
						Destination = destination
					})
				.ToArray()
		};

		using (context.OfflineScope())
		{
			backup.Files.ForEach(f =>
			{
				f.Backup = f.Source + "." + backupName;
				var shellOutput = context.MasterConnector().ExecuteShellCommandOnServer($@"COPY ""{f.Source}"" ""{f.Backup}""");
				if (!shellOutput.Contains("1 file(s) copied."))
					throw new Exception();
			});
		}

		var file = Path.Combine(BackupNameBuilder.TempFolder(), backupName);
		File.WriteAllText(file, JsonConvert.SerializeObject(backup, Formatting.Indented));
	}

	public bool TryRestore(ICacheContext context)
	{
		return fileCopyRestore(context, BackupName(context));
	}

	private static bool fileCopyRestore(ICacheContext context, string backupName)
	{
		var file = Path.Combine(BackupNameBuilder.TempFolder(), backupName);

		if (!File.Exists(file))
		{
			context.LogInfo($@"Backup file {file} not found");
			return false;
		}

		var backup = JsonConvert.DeserializeObject<Backup>(File.ReadAllText(file));

		if (!context.DatabaseExists())
			return restoreToNewDatabase(backup, context);

		using (context.OfflineScope())
			return copyBackup(backup, context);
	}

	private static bool restoreToNewDatabase(Backup backup, ICacheContext context)
	{
		if (!copyBackup(backup, context))
			return false;

		var filesToRestore = string.Join(",",
			backup
				.Files
				.Select(x => x.Destination.Replace("$(DatabaseName)", context.DatabaseName()))
				.Select(destination => $@"( FILENAME = N'{destination}' )")
				.ToArray()
		);

		context.MasterConnector().Execute($@"
					CREATE DATABASE [{context.DatabaseName()}] ON
						{filesToRestore}
					FOR ATTACH;");

		return true;
	}

	private static bool copyBackup(Backup backup, ICacheContext context)
	{
		return backup.Files.All(f =>
		{
			var destination = f.Destination.Replace("$(DatabaseName)", context.DatabaseName());
			var command = $@"COPY ""{f.Backup}"" ""{destination}""";
			var shellOutput = context.MasterConnector().ExecuteShellCommandOnServer(command);
			var result = (shellOutput.Contains("1 file(s) copied."));
			context.LogInfo($@"copyBackup {command} returned {shellOutput}");
			return result;
		});
	}

	private static void sqlServerFileCopyInvalidation(ICacheContext context, IEnumerable<string> prefixes)
	{
		var files = new DirectoryInfo(BackupNameBuilder.TempFolder()).GetFiles($"*{BackupNameBuilder.Extension()}");
		var invalidated = from f in files
			from p in prefixes
			where f.Name.StartsWith(p)
			select f;

		var invalidatedBackups = from fileName in invalidated
			let file = fileName.FullName
			let backup = JsonConvert.DeserializeObject<Backup>(File.ReadAllText(file))
			select new
			{
				file,
				backup
			};

		invalidatedBackups.ForEach(x =>
		{
			deleteDatabaseCopies(context, x.backup);
			File.Delete(x.file);
		});
	}

	private static void deleteDatabaseCopies(ICacheContext context, Backup backup)
	{
		backup.Files.ForEach(f =>
		{
			var shellOutput = context.MasterConnector().ExecuteShellCommandOnServer($@"DEL ""{f.Backup}""");

			// for delete, nothing means success!!
			if (string.IsNullOrEmpty(shellOutput))
				return;

			// actual backup gone? manual or by another process? weird...
			if (shellOutput.Contains("The system cannot find the file specified."))
				return;
			if (shellOutput.Contains("Could Not Find"))
				return;

			// actual database backup file locked by another process? weird. but if so, assume its being deleted.
			if (shellOutput.Contains("The process cannot access the file because it is being used by another process."))
				return;

			throw new Exception("Deletion failed with result: " + shellOutput);
		});
	}

}
