namespace DbReset;

public class CacheOptions
{
	public string ConnectionString { get; set; }
	public string Key { get; set; }
	public string Version { get; set; }
	public IOutput Output { get; set; }
	public ICacheStrategy Strategy { get; set; }

	public bool OptimizePostgreSqlForFastTesting { get; set; } = false;
}
