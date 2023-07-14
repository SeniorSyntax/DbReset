using System;
using System.Collections.Generic;
using System.Linq;

namespace DbReset.Internals;

internal class PostgresDatabaseTemplateStrategy : ICacheStrategy
{
	public void Backup(ICacheContext context)
	{
		postgresDatabaseTemplateBackup(context, BackupName(context));
	}

	public void Invalidate(ICacheContext context, IEnumerable<string> prefixes)
	{
		var databaseNames = context.MasterConnector().Query<string>($"SELECT datname FROM pg_catalog.pg_database");
		var invalidated = from db in databaseNames
			where db.EndsWith(BackupNameBuilder.Extension())
			from p in prefixes
			where db.StartsWith(p)
			select db;

		invalidated.ForEach(x =>
		{
			new DatabaseDropper().Drop(context.MasterConnector(), x);
		});
	}

	public string BackupName(ICacheContext context)
	{
		return BackupNameBuilder.GetNameWithMaxLength(context.Key(), context.Version(), 63);
	}

	private static void postgresDatabaseTemplateBackup(ICacheContext context, string backupName)
	{
		var databaseName = context.DatabaseName();
		var connector = context.MasterConnector();

		new DatabaseDropper().Drop(connector, backupName);

		connector.DisconnectAllUsersFrom(databaseName);
		connector.Execute($@"ALTER DATABASE ""{databaseName}"" RENAME TO ""{backupName}""");

		// execute creation with 5 minute timeout.
		// experiment that may fix randomness in monolith infra tests
		// they seems to take time... at times.
		var sql = $@"CREATE DATABASE ""{databaseName}"" WITH TEMPLATE ""{backupName}""";
		var connectorWithTimeout = context.MasterConnector() as ISqlExecuteWithTimeout;
		connectorWithTimeout.ExecuteWithTimeout(sql, (int) TimeSpan.FromMinutes(5).TotalSeconds);
	}

	public bool TryRestore(ICacheContext context)
	{
		return templateRestore(context, BackupName(context));
	}

	private static bool templateRestore(ICacheContext context, string backupName)
	{
		var exists = context.MasterConnector()
			.ExecuteScalar<bool>($"SELECT 1 FROM pg_catalog.pg_database WHERE datname = '{backupName}'");
		if (!exists)
			return false;

		var databaseName = context.DatabaseName();

		context.MasterConnector().DisconnectAllUsersFrom(backupName);
		new DatabaseDropper().Drop(context.MasterConnector(), databaseName);

		// execute creation with 5 minute timeout.
		// experiment that may fix randomness in monolith infra tests
		// they seems to take time... at times.
		var sql = $@"CREATE DATABASE ""{databaseName}"" WITH TEMPLATE ""{backupName}""";
		var connectorWithTimeout = context.MasterConnector() as ISqlExecuteWithTimeout;
		connectorWithTimeout.ExecuteWithTimeout(sql, (int) TimeSpan.FromMinutes(5).TotalSeconds);

		return true;
	}
}
