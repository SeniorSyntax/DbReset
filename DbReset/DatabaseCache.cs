using System.IO;
using DbAgnostic;
using DbReset.Internals;

namespace DbReset;

public static class DatabaseCache
{
	public static void Store(CacheOptions options)
	{
		ICacheContext context = new CacheContext
		{
			ConnectionString = options.ConnectionString,
			Key = options.Key,
			Version = options.Version,
			Output = options.Output
		};

		Directory.CreateDirectory(BackupNameBuilder.TempFolder());

		var strategy = cacheStrategy(options);
		new DatabaseCacheInvalidator().Invalidate(context, strategy);
		strategy.Backup(context);
	}

	public static bool TryReset(CacheOptions options)
	{
		ICacheContext context = new CacheContext
		{
			ConnectionString = options.ConnectionString,
			Key = options.Key,
			Version = options.Version,
			Output = options.Output
		};

		return cacheStrategy(options).TryRestore(context);
	}

	private static ICacheStrategy cacheStrategy(CacheOptions options)
	{
		if (options.Strategy != null)
			return options.Strategy;
		return options.ConnectionString.PickFunc<ICacheStrategy>(
			() => new SqlServerFileStrategy(),
			() => new PostgresDatabaseTemplateStrategy()
		);
	}
}
