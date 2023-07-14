using DbAgnostic;
using Npgsql;

namespace DbReset.Internals;

internal class DatabaseDropper
{
	public void Drop(ISqlConnector onMaster, string databaseName)
	{
		onMaster.PickAction(
			() =>
			{
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
					IF EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = N'{databaseName}')
						ALTER DATABASE [{databaseName}] SET ONLINE
				");

				onMaster.Execute($@"
					IF EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = N'{databaseName}')
						DROP DATABASE [{databaseName}]
				");

			},
			() =>
			{
				onMaster.DisconnectAllUsersFrom(databaseName);
				onMaster.Execute($@"DROP DATABASE IF EXISTS ""{databaseName}"" WITH (FORCE)");
				NpgsqlConnection.ClearAllPools();
			});
	}
}
