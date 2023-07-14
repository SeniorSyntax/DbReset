using System;

namespace DbReset.Internals;

internal static class ConnectorExtensions
{
	public static string ExecuteShellCommandOnServer(this ISqlConnector connector, string command)
	{
		string result = null;
		connector.Batch(b =>
		{
			b.Execute("EXEC sp_configure 'show advanced options', 1");
			b.Execute("RECONFIGURE");
			b.Execute("EXEC sp_configure 'xp_cmdshell', 1");
			b.Execute("RECONFIGURE");
			var lines = b.Query<string>("xp_cmdshell '" + command + "'");
			result = string.Join(Environment.NewLine, lines);
		});
		return result;
	}

	public static void DisconnectAllUsersFrom(this ISqlConnector connector, string databaseName)
	{
		connector.Execute($@"
			SELECT
				pg_terminate_backend (pg_stat_activity.pid)
			FROM
				pg_stat_activity
			WHERE
				pg_stat_activity.datname = '{databaseName}';
			");
	}
}
