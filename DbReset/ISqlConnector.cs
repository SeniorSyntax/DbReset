using System;
using System.Data;

namespace DbReset;

public interface ISqlConnector : ISqlExecute
{
	bool TryConnection();
	void Transaction(Action<ISqlExecute> action);
	void Batch(Action<ISqlExecute> action);
	void Execute(Action<IDbConnection> action);
}
