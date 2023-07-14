using System.Collections;
using System.Linq;

namespace DbReset.Internals;

internal class DatabaseCacheInvalidator
{
	private static readonly Hashtable invalidationRanFor = new();

	public void Invalidate(ICacheContext context, ICacheStrategy cacheStrategy)
	{
		var runOncePer = cacheStrategy.BackupName(context);

		var alreadyInvalidated = (bool)(invalidationRanFor[runOncePer] ?? false);
		if (alreadyInvalidated)
			return;
		invalidationRanFor[runOncePer] = true;

		var prefixes = BackupNameBuilder.PossibleKeysForKey(context.Key()).ToArray();

		cacheStrategy.Invalidate(context, prefixes);
	}
}
