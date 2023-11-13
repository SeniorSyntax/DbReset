using DbAgnostic;

namespace DbReset.Internals;

internal class PostgresServerOptimization
{
	public void Apply(ICacheContext context)
	{
		var connector = context.DatabaseConnector();
		connector.PickAction(() =>
		{
			// nothing for sql server
		}, () =>
		{
			// postgres optimization
			// https://stackoverflow.com/questions/9407442/optimise-postgresql-for-fast-testing
			connector.Execute("ALTER SYSTEM SET fsync TO 'off';");
			connector.Execute("ALTER SYSTEM SET full_page_writes TO 'off';");
			var shared_buffers = 512 * 1000 / 8; // 512mb in unit of 8kb =  64000
			connector.Execute($"ALTER SYSTEM SET shared_buffers TO '{shared_buffers}';");
			var work_mem = 512 * 1000; // 512mb in kb = 512000
			connector.Execute($"ALTER SYSTEM SET work_mem TO '{work_mem}';");
			connector.Execute("select pg_reload_conf();");
		});
	}
}