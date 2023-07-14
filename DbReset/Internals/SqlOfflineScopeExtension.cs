using System;
using System.Data.SqlClient;

namespace DbReset.Internals;

internal static class SqlOfflineScopeExtension
{
	public static IDisposable OfflineScope(this ICacheContext context)
	{
		SqlConnection.ClearAllPools();
		context.MasterConnector().Execute($"ALTER DATABASE [{context.DatabaseName()}] SET OFFLINE WITH ROLLBACK IMMEDIATE");
		return new GenericDisposable(() =>
		{
			//
			context.MasterConnector().Execute($"ALTER DATABASE [{context.DatabaseName()}] SET ONLINE");
		});
	}
}
