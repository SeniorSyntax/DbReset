using System.Collections.Generic;
using Dapper;
using DbAgnostic;

namespace DbReset.Test;

public static class ExecuteExtensions
{
	public static void Execute(this string connectionString, string sql)
	{
		using var connection = connectionString.CreateConnection();
		connection.Execute(sql);
	}

	public static T ExecuteScalar<T>(this string connectionString, string sql)
	{
		using var connection = connectionString.CreateConnection();
		return connection.ExecuteScalar<T>(sql);
	}

	public static IEnumerable<T> Query<T>(this string connectionString, string sql)
	{
		using var connection = connectionString.CreateConnection();
		return connection.Query<T>(sql);
	}
}
