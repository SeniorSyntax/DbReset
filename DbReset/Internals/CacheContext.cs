using DbAgnostic;

namespace DbReset.Internals;

internal class CacheContext : ICacheContext
{
	public string ConnectionString { get; set; }
	public string Key { get; set; }
	public string Version { get; set; }

	public string DatabaseName { get; set; }
	public ISqlConnector DatabaseConnector { get; set; }
	public ISqlConnector MasterConnector { get; set; }

	public IOutput Output { get; set; }


	string ICacheContext.DatabaseName() => DatabaseName ?? ConnectionString.DatabaseName();

	string ICacheContext.Key() => Key;
	string ICacheContext.Version() => Version;

	ISqlConnector ICacheContext.DatabaseConnector() => DatabaseConnector ?? new DapperConnector(ConnectionString);
	ISqlConnector ICacheContext.MasterConnector() => MasterConnector ?? new DapperConnector(ConnectionString.PointToMasterDatabase());

	public void LogInfo(string message) => Output?.Info(message);
	
	private static bool? _dbRunsOnWindows;
	public string TempFolder()
	{
		_dbRunsOnWindows ??= ((ICacheContext)this).MasterConnector()
			.ExecuteScalar<string>("select host_platform from sys.dm_os_host_info;") == "Windows";
		
		return _dbRunsOnWindows.Value ? 
			DatabaseTempFolders.ForWindows : 
			DatabaseTempFolders.ForLinux;
	}
}
