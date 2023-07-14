using System.Collections.Generic;
using DbAgnostic;

namespace DbReset;

public interface ISqlExecute : IDbSelector
{
	void Execute(string sql);
	void Execute(string sql, object param);
	T ExecuteScalar<T>(string sql);
	T ExecuteScalar<T>(string sql, object param);
	IEnumerable<T> Query<T>(string sql);
}
