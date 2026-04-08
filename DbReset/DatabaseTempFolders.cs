namespace DbReset;

public static class DatabaseTempFolders
{
    public static string ForWindows => @"c:\temp\dbcache";
    public static string ForLinux => "/tmp/dbcache";
}