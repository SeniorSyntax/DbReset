using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Dapper;
using DbAgnostic;
using Polly;

namespace DbReset.Internals;

internal class DapperConnector : ISqlConnector, ISqlExecuteWithTimeout
{
	private readonly string _connectionString;
	private readonly DatabaseRetries _databaseRetries;

	internal DapperConnector(string connectionString)
	{
		_databaseRetries = new DatabaseRetries();
		_connectionString = connectionString;
	}

	private DbConnection open() =>
		_connectionString.CreateConnection();

	public bool TryConnection()
	{
		try
		{
			using (var connection = open())
				connection.Execute("select 1");
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public void Execute(string sql)
	{
		_databaseRetries.HandleWithRetry(context =>
		{
			DatabaseRetries.SetSql(context, sql);
			using var connection = open();
			connection.Execute(sql);
		});
	}

	public void ExecuteWithTimeout(string sql, int commandTimeout)
	{
		_databaseRetries.HandleWithRetry(context =>
		{
			DatabaseRetries.SetSql(context, sql);
			using var connection = open();
			connection.Execute(sql, commandTimeout: commandTimeout);
		});
	}

	public void Execute(string sql, object param)
	{
		_databaseRetries.HandleWithRetry(context =>
		{
			DatabaseRetries.SetSql(context, sql);
			using var connection = open();
			connection.Execute(sql, param);
		});
	}

	public T ExecuteScalar<T>(string sql)
	{
		T result = default;
		_databaseRetries.HandleWithRetry(context =>
		{
			DatabaseRetries.SetSql(context, sql);
			using var connection = open();
			result = connection.ExecuteScalar<T>(sql);
		});
		return result;
	}

	public T ExecuteScalar<T>(string sql, object param)
	{
		T result = default;
		_databaseRetries.HandleWithRetry(context =>
		{
			DatabaseRetries.SetSql(context, sql);
			using var connection = open();
			result = connection.ExecuteScalar<T>(sql, param);
		});
		return result;
	}

	public IEnumerable<T> Query<T>(string sql)
	{
		var result = default(IEnumerable<T>);
		_databaseRetries.HandleWithRetry(context =>
		{
			DatabaseRetries.SetSql(context, sql);
			using var connection = open();
			result = connection.Query<T>(sql);
		});
		return result;
	}

	public void Execute(Action<IDbConnection> action)
	{
		_databaseRetries.HandleWithRetry(context =>
		{
			using var connection = open();
			connection.Open();
			action.Invoke(connection);
		});
	}

	public void Transaction(Action<ISqlExecute> action)
	{
		_databaseRetries.HandleWithRetry(context =>
		{
			using var c = _connectionString.CreateConnection();
			c.Open();
			using var t = c.BeginTransaction();
			var b = new BatchOrTransaction(context, this, c, t);
			action.Invoke(b);
			t.Commit();
		});
	}

	public void Batch(Action<ISqlExecute> action)
	{
		_databaseRetries.HandleWithRetry(context =>
		{
			using var c = _connectionString.CreateConnection();
			var b = new BatchOrTransaction(context, this, c, null);
			action.Invoke(b);
		});
	}

	internal class BatchOrTransaction : ISqlExecute, ISqlExecuteWithTimeout
	{
		private readonly IDbConnection _connection;
		private readonly Context _retryContext;
		private readonly ISqlConnector _connector;
		private readonly IDbTransaction _transaction;

		internal BatchOrTransaction(Context retryContext, ISqlConnector connector, IDbConnection connection, IDbTransaction transaction)
		{
			_retryContext = retryContext;
			_connector = connector;
			_connection = connection;
			_transaction = transaction;
		}

		public void Execute(string sql)
		{
			DatabaseRetries.SetSql(_retryContext, sql);
			_connection.Execute(sql, transaction: _transaction);
		}

		public void Execute(string sql, object param)
		{
			DatabaseRetries.SetSql(_retryContext, sql);
			_connection.Execute(sql, param, _transaction);
		}

		public void ExecuteWithTimeout(string sql, int commandTimeout)
		{
			DatabaseRetries.SetSql(_retryContext, sql);
			_connection.Execute(sql, transaction: _transaction, commandTimeout: commandTimeout);
		}

		public T ExecuteScalar<T>(string sql)
		{
			DatabaseRetries.SetSql(_retryContext, sql);
			return _connection.ExecuteScalar<T>(sql, transaction: _transaction);
		}

		public T ExecuteScalar<T>(string sql, object param)
		{
			DatabaseRetries.SetSql(_retryContext, sql);
			return _connection.ExecuteScalar<T>(sql, param, _transaction);
		}

		public IEnumerable<T> Query<T>(string sql)
		{
			DatabaseRetries.SetSql(_retryContext, sql);
			return _connection.Query<T>(sql, transaction: _transaction);
		}

		public T PickFunc<T>(Func<T> sqlServer, Func<T> postgres) =>
			_connector.PickFunc(sqlServer, postgres);
	}

	public T PickFunc<T>(Func<T> sqlServer, Func<T> postgres) =>
		_connectionString.ToDbSelector().PickFunc(sqlServer, postgres);
}
