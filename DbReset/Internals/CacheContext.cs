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
}
