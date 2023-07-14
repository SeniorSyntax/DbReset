namespace DbReset.Test;

public static class TestConnectionString
{
#if DEBUG
	public const string ConnectionString =
		"Data Source=.;Integrated Security=True;Initial Catalog=DbReset.Test;";
#else
	public const string ConnectionString =
		@"Data Source=.\SQLEXPRESS;Integrated Security=True;Initial Catalog=DbReset.Test;";
#endif
}
