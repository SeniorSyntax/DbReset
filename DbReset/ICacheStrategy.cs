using System.Collections.Generic;

namespace DbReset;

public interface ICacheStrategy
{
	void Backup(ICacheContext context);
	bool TryRestore(ICacheContext context);
	void Invalidate(ICacheContext context, IEnumerable<string> prefixes);

	string BackupName(ICacheContext context);
}
