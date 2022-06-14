using System.Data.SqlClient;
using System.Text.RegularExpressions;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using NPoco;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// A database manager used to communicate with a SQL Server Database
/// </summary>
public class SqlServerDbManager: DbManager
{
    public SqlServerDbManager(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public override bool UseTop => true;

    protected override string JsonExtractMethod => "json_value";

    protected override IDatabase GetDb()
    {
        return new NPoco.Database(ConnectionString,
            null,
            SqlClientFactory.Instance);
    }

    protected override DbCreateResult CreateDatabase(bool recreate)
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString);
        string dbName = builder["Database"].ToString()?.EmptyAsNull() ?? "FileFlows";
        builder["Database"] = null;

        string connString = builder.ConnectionString;
        
        using var db = new NPoco.Database(connString, null, SqlClientFactory.Instance);
        bool exists = string.IsNullOrEmpty(db.ExecuteScalar<string>("select name from sys.databases where name = @0", dbName)) == false;
        if (exists)
        {
            if(recreate == false)
                return DbCreateResult.AlreadyExisted;
            Logger.Instance.ILog("Dropping existing database");
            db.Execute($"drop database {dbName}");
        }

        Logger.Instance.ILog("Creating Database");
        db.Execute("create database " + dbName);
        exists = string.IsNullOrEmpty(db.ExecuteScalar<string>("select name from sys.databases where name = @0", dbName)) == false;
        return exists ? DbCreateResult.Created : DbCreateResult.Failed;
    }

    protected override void CreateStoredProcedures()
    {
        Logger.Instance.ILog("Creating Stored Procedures");
        using var db = new NPoco.Database(ConnectionString, null, SqlClientFactory.Instance);

        var scripts = GetStoredProcedureScripts("SqlServer");
        foreach (var script in scripts)
        {
            Logger.Instance.ILog("Creating script: " + script.Key);
            
            foreach (string sql in script.Value.Split(new string[] { "\nGO" },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                var sqlScript = sql.Replace("@", "@@").Trim();
                db.Execute(sqlScript);
            }
        }
    }

    protected override bool CreateDatabaseStructure()
    {
        Logger.Instance.ILog("Creating Database Structure");

        string createDbSql = CreateDbScript.Replace("current_timestamp", "getdate()")
            .Replace("VARCHAR(36)", "uniqueidentifier")
            .Replace("TEXT", "varchar(max)");
        using var db = new NPoco.Database(ConnectionString, null, SqlClientFactory.Instance);
        db.Execute(createDbSql);

        db.Execute("CREATE INDEX idx_DbObject_Type_Name ON DbObject(Type, Name);");
        db.Execute("ALTER DATABASE CURRENT SET MEMORY_OPTIMIZED_ELEVATE_TO_SNAPSHOT=ON");
        return true;
    }


    /// <summary>
    /// Gets the library file status  
    /// </summary>
    /// <returns></returns>
    public override async Task<IEnumerable<LibraryStatus>> GetLibraryFileOverview()
    {
        int quarter = TimeHelper.GetCurrentQuarter();
        using var db = GetDb();
        return await db.FetchAsync<LibraryStatus>(
            "exec GetLibraryFiles @@Status=0, @@IntervalIndex=@0, @@MaxItems=0, @@Start=0, @@NodeUid=null, @@Overview=1",
            quarter);
    }

    /// <summary>
    /// Gets the library file with the corresponding status
    /// </summary>
    /// <param name="status">the library file status</param>
    /// <param name="start">the row to start at</param>
    /// <param name="max">the maximum items to return</param>
    /// <param name="quarter">the current quarter</param>
    /// <param name="nodeUid">optional UID of node to limit results for</param>
    /// <returns>an enumerable of library files</returns>
    public override async Task<IEnumerable<LibraryFile>> GetLibraryFiles(FileStatus status, int start, int max, int quarter, Guid? nodeUid)
    {
        using var db = GetDb();
        var dbObjects = await db.FetchAsync<DbObject>("exec GetLibraryFiles @@Status=@1, @@IntervalIndex=@1, @@MaxItems=@2, @@Start=@3, @@NodeUid=@4, @@Overview=0", 
            (int)status, quarter, max, start, nodeUid);
        return ConvertFromDbObject<LibraryFile>(dbObjects);
    }
    
    /// <summary>
    /// Gets the shrinkage group data
    /// </summary>
    /// <returns>the shrinkage group data</returns>
    public override Task<IEnumerable<ShrinkageData>> GetShrinkageGroups()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the failure flow for a particular library
    /// </summary>
    /// <param name="libraryUid">the UID of the library</param>
    /// <returns>the failure flow</returns>
    public override async Task<Flow> GetFailureFlow(Guid libraryUid)
    {
        using var db = GetDb();
        var dbObject = await db.SingleAsync<DbObject>(
            "select * from DbObject where Type = @0 " +
            "and json_value(Data,'$.Type') = @1 " +
            "and json_value(Data,'$.Enabled') = 1 ",
            typeof(Flow).FullName, (int)
            FlowType.Failure);
        return ConvertFromDbObject<Flow>(dbObject);
    }
    
    /// <summary>
    /// Tests the connection to a database
    /// </summary>
    /// <param name="server">the server address</param>
    /// <param name="name">the database name</param>
    /// <param name="user">the connecting user</param>
    /// <param name="password">the password to use</param>
    /// <returns>any error or empty string if successful</returns>
    public string Test(string server, string name, string user, string password)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder();
            builder["Server"] = server;
            builder["User"] = user;
            builder["Password"] = password;
            string connString = builder.ConnectionString;
            using var db = new NPoco.Database(connString, null, SqlClientFactory.Instance);
            bool exists = string.IsNullOrEmpty(db.ExecuteScalar<string>("select name from sys.databases where name = @0", name)) == false;
            return string.Empty;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// Gets a database connection string
    /// </summary>
    /// <param name="server">the database server</param>
    /// <param name="database">the database name</param>
    /// <param name="user">the connecting user</param>
    /// <param name="password">the password for the connection</param>
    /// <returns>the database connection string</returns>
    public string GetConnectionString(string server, string database, string user, string password)
    {
        var builder = new SqlConnectionStringBuilder();
        builder["Server"] = server;
        builder["Database"] = database?.EmptyAsNull() ?? "FileFlows";
        builder["User"] = user;
        builder["Password"] = password;
        return builder.ConnectionString;
    }

    /// <summary>
    /// Populates the database settings from a connection string
    /// </summary>
    /// <param name="settings">the settings to populate</param>
    /// <param name="connectionString">the connection string to parse</param>
    internal void PopulateSettings(SettingsUiModel settings, string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        settings.DbType = DatabaseType.SqlServer;
        settings.DbServer = builder["Server"].ToString();
        settings.DbName = builder["Database"].ToString();
        settings.DbUser = builder["User"].ToString();
        settings.DbPassword = builder["Password"].ToString();
    }
}