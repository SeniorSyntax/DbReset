using System.IO;
using DbReset.Internals;

namespace DbReset;

public static class DatabaseCache
{
	public static void Store(CacheOptions options)
	{
		var context = new CacheContext
		{
			ConnectionString = options.ConnectionString,
			Key = options.Key,
			Version = options.Version,
			Output = options.Output
		};

		Directory.CreateDirectory(context.TempFolder());

		if (options.OptimizePostgreSqlForFastTesting)
			new PostgresServerOptimization().Apply(context);
		
		var strategy = cacheStrategy(options.Strategy, context);
		new DatabaseCacheInvalidator().Invalidate(context, strategy);
		strategy.Backup(context);
	}

	public static bool TryReset(CacheOptions options)
	{
		var context = new CacheContext
		{
			ConnectionString = options.ConnectionString,
			Key = options.Key,
			Version = options.Version,
			Output = options.Output
		};

		return cacheStrategy(options.Strategy, context).TryRestore(context);
	}

	private static ICacheStrategy cacheStrategy(ICacheStrategy choosenStrategy, ICacheContext context)
	{
		if (choosenStrategy != null)
			return choosenStrategy;
		return context.DatabaseConnector().PickFunc<ICacheStrategy>(
			() =>
			{
				if(DatabaseRunsOn.Windows(context))
					return new SqlServerFileStrategy();
				return new SqlServerSnapshotStrategy_Experimental();
			},
			() => new PostgresDatabaseTemplateStrategy()
		);
	}
}