namespace DbReset;

public interface ICacheContext
{
	ISqlConnector DatabaseConnector();
	ISqlConnector MasterConnector();
	string DatabaseName();

	string Key();
	string Version();

	void LogInfo(string message);
}
