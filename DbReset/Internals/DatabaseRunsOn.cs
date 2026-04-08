namespace DbReset.Internals;

internal static class DatabaseRunsOn
{
    private static bool? _dbRunsOnWindows;
    internal static bool Windows(ICacheContext context)
    {
        if (!_dbRunsOnWindows.HasValue)
        {
            var connector = context.MasterConnector();
            _dbRunsOnWindows = connector.PickFunc(
                () => connector.ExecuteScalar<string>("select host_platform from sys.dm_os_host_info;") == "Windows",
                () => !connector.ExecuteScalar<string>("SELECT version();").Contains("linux")
            );
        }
        return _dbRunsOnWindows.Value;
    }
    
}