using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Npgsql;
using Polly;
using Polly.Retry;

namespace DbReset.Internals;

internal class DatabaseRetries
{
	private static readonly RetryPolicy retryPolicy =
		Policy.Handle<TimeoutException>()
			.Or<NpgsqlException>(e => e.InnerException is TimeoutException)
			.Or<SqlException>(SqlTransientException.IsTransient)
			.OrInner<SqlException>(SqlTransientException.IsTransient)
			.WaitAndRetry(5, i => TimeSpan.FromSeconds(Math.Pow(2, i)),
				(exception, span, retries, context) =>
				{
					if (!context.ContainsKey("failures"))
						context.Add("failures", new List<Exception>());
					(context["failures"] as List<Exception>).Add(exception);
				});

	public static void SetSql(Context context, string sql)
	{
		Set(context, "sql", sql);
	}

	public static void Set(Context context, string key, object value)
	{
		if (!context.ContainsKey(key))
			context.Add(key, value);
		context[key] = value;
	}

	private static T get<T>(Context context, string key, T @default)
	{
		if (context.TryGetValue(key, out var value))
			return (T)value;
		return @default;
	}

	internal void HandleWithRetry(Action<Context> action)
	{
		var context = new Context();

		try
		{
			retryPolicy.Execute(c => action.Invoke(context), context);
		}
		catch (Exception ex)
		{
			var exceptions = get(context, "failures", new[] { ex }.AsEnumerable());
			var failingSql = get(context, "sql", "<No sql script>");

			var logMessage = new StringBuilder();

			logMessage.AppendLine($"Script failed: {failingSql}");

			exceptions.ForEach(exception =>
			{
				if (exception is SqlException sqlException)
				{
					logMessage.AppendLine(exception.Message);
					var sqlErrorNumbers = string.Join(", ", sqlException.Errors.OfType<SqlError>().Select(e => e.Number));
					logMessage.AppendLine($"Error code: {sqlException.ErrorCode}");
					logMessage.AppendLine($"Sql error numbers: {sqlErrorNumbers}");
				}

				logMessage.AppendLine(exception.ToString());
			});

			throw new Exception(logMessage.ToString(), ex);
		}
	}
}
