using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DbReset;

public class SqlServerSnapshotStrategy_Experimental : ICacheStrategy
{
	public void Backup(ICacheContext context)
	{
		var backupName = BackupName(context);

		var files = context.DatabaseConnector()
			.Query<(string name, string filename)>("select name, filename from sys.sysfiles");
		var filesSqls = files
			.Where(x => x.filename.EndsWith(".mdf") || x.filename.EndsWith("ndf"))
			.Select(x => $@"
(
    NAME = [{x.name}], FILENAME = '{Path.Combine(BackupNameBuilder.TempFolder(), new FileInfo(x.filename).Name)}'
)
");
		var filesSql = string.Join(",", filesSqls);

		context.DatabaseConnector()
			.Execute($@"
CREATE DATABASE [{backupName}]
ON
{filesSql}
AS SNAPSHOT OF [{context.DatabaseName()}]
");
	}

	public bool TryRestore(ICacheContext context)
	{
		var backupName = BackupName(context);
		var databaseName = context.DatabaseName();

		var onMaster = context.MasterConnector();

		var databaseId = onMaster
			.Query<int?>($"select database_id from sys.databases where name = '{databaseName}'")
			.SingleOrDefault();
		var snapshots = databaseId.HasValue ?
			onMaster.Query<string>($"select name from sys.databases where source_database_id = {databaseId}") :
			Enumerable.Empty<string>();

		if (!snapshots.Any())
			return false;

		onMaster.Execute($@"
			DECLARE @kill varchar(8000) = '';
			SELECT @kill = @kill + 'kill ' + CONVERT(varchar(5), spid) + ';'
			FROM master..sysprocesses  p
			INNER JOIN master.sys.dm_exec_sessions s ON s.session_id = p.spid
			WHERE dbid = db_id('{databaseName}')
			AND s.is_user_process = 1
			EXEC(@kill);
		");

		onMaster.Execute($@"
			RESTORE DATABASE [{databaseName}]
			FROM DATABASE_SNAPSHOT = '{backupName}'
		");

		return true;
	}

	public void Invalidate(ICacheContext context, IEnumerable<string> prefixes)
	{
	}

	public string BackupName(ICacheContext context) =>
		BackupNameBuilder.GetNameWithMaxLength(context.Key(), context.Version(), int.MaxValue);
}
