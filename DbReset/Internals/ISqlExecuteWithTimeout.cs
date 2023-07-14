namespace DbReset.Internals;

internal interface ISqlExecuteWithTimeout
{
	void ExecuteWithTimeout(string sql, int commandTimeout);
}
