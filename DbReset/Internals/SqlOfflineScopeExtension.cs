using System;
using System.Data.SqlClient;

namespace DbReset.Internals;

internal static class SqlOfflineScopeExtension
{
	public static IDisposable OfflineScope(this ICacheContext context)
	{
		var databaseName = context.DatabaseName();
		var killSql = $@"
DECLARE @kills nvarchar(max) = N'';

SELECT @kills = @kills + N'KILL ' + CAST(session_id AS nvarchar(10)) + N';'
FROM sys.dm_exec_sessions
WHERE is_user_process = 1
  AND session_id <> @@SPID
  AND database_id = DB_ID(N'{databaseName}');

IF (@kills <> N'')
    EXEC sp_executesql @kills;
";
		context.MasterConnector().Execute(killSql);
		context.MasterConnector().Execute($"ALTER DATABASE [{context.DatabaseName()}] SET OFFLINE WITH ROLLBACK IMMEDIATE");
		return new GenericDisposable(() =>
		{
			context.MasterConnector().Execute($"ALTER DATABASE [{context.DatabaseName()}] SET ONLINE");
		});
	}
}
