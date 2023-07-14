namespace DbReset.Internals;

internal static class DatabaseExistsExtension
{
	public static bool DatabaseExists(this ICacheContext context) =>
		context
			.MasterConnector()
			.ExecuteScalar<int>($"SELECT COUNT(*) FROM master.dbo.sysdatabases WHERE name = N'{context.DatabaseName()}'") != 0;
}
