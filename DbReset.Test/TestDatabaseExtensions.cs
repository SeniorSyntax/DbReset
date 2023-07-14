using System.Linq;
using Dapper;
using DbAgnostic;
using Npgsql;

namespace DbReset.Test;

public static class TestDatabaseExtensions
{
	public static void RecreateTestDatabase(this string connectionString)
	{
		connectionString.DropTestDatabase();
		connectionString.CreateTestDatabase();
	}

	public static void CreateTestDatabase(this string connectionString)
	{
		var databaseName = connectionString.DatabaseName();
		var sqlServer = $"CREATE DATABASE [{databaseName}]";
		var postgres = $@"CREATE DATABASE ""{databaseName}""";
		var sql = connectionString.PickDialect(sqlServer, postgres);

		using var connection = connectionString
			.PointToMasterDatabase()
			.CreateConnection();
		connection.Execute(sql);

		connection.PickAction(() =>
		{
			var dataPath = connection.Query<string>("SELECT CONVERT(sysname, SERVERPROPERTY('InstanceDefaultDataPath'))").Single();

			connection.Execute($@"
				ALTER DATABASE [{databaseName}]
				ADD FILE (
					NAME = [{databaseName}_AnotherFile],
					FILENAME = '{dataPath}\{databaseName}_AnotherFile.ndf'
				)");
		}, () => { });
	}

	public static void DropTestDatabase(this string connectionString)
	{
		var databaseName = connectionString.DatabaseName();
		using var connection = connectionString
			.PointToMasterDatabase()
			.CreateConnection();

		connection.PickAction(() =>
		{
			var databaseId = connection
				.Query<int?>($"select database_id from sys.databases where name = '{databaseName}'")
				.SingleOrDefault();
			var snapshots = databaseId.HasValue ? connection.Query<string>($"select name from sys.databases where source_database_id = {databaseId}") : Enumerable.Empty<string>();

			snapshots.ForEach(x => { connection.Execute($@"DROP DATABASE [{x}]"); });

			connection.Execute($@"
				DECLARE @kill varchar(8000) = '';
				SELECT @kill = @kill + 'kill ' + CONVERT(varchar(5), spid) + ';'
				FROM master..sysprocesses  p
				INNER JOIN master.sys.dm_exec_sessions s ON s.session_id = p.spid
				WHERE dbid = db_id('{databaseName}')
				AND s.is_user_process = 1
				EXEC(@kill);
				");

			connection.Execute($@"
				IF EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = N'{databaseName}')
					DROP DATABASE [{databaseName}]
				");
		}, () =>
		{
			connection.Execute($@"DROP DATABASE IF EXISTS ""{databaseName}"" WITH (FORCE)");
			NpgsqlConnection.ClearAllPools();
		});
	}
}
