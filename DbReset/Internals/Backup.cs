using System.Collections.Generic;

namespace DbReset.Internals;

internal class Backup
{
	public IEnumerable<BackupFile> Files { get; set; }
}
