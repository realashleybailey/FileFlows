using System.Numerics;
using System.Text.RegularExpressions;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using NPoco;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// A database manager used to communicate with a MySql/MariaDB Database
/// </summary>
public class MySqlDbManager: DbManager
{
    protected readonly string CreateMySqlDbScript =
        @$"CREATE TABLE {nameof(DbObject)}(
            Uid             VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL          PRIMARY KEY,
            Name            VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
            Type            VARCHAR(255)       COLLATE utf8_unicode_ci      NOT NULL,
            DateCreated     datetime           default           now(),
            DateModified    datetime           default           now(),
            Data            MEDIUMTEXT         COLLATE utf8_unicode_ci      NOT NULL
        );";
    /// <summary>
    /// Creates an instance of a MySqlDbManager
    /// </summary>
    /// <param name="connectionString">a mysql connection string</param>
    public MySqlDbManager(string connectionString)
    {
        ConnectionString = connectionString;
    }
    
    protected override IDatabase GetDb()
    {
        return new NPoco.Database(ConnectionString,
            null,
            MySqlConnector.MySqlConnectorFactory.Instance);
    }

    protected override DbCreateResult CreateDatabase(bool recreate)
    {
        string connString = Regex.Replace(ConnectionString, "(^|;)Database=[^;]+", "");
        if (connString.StartsWith(";"))
            connString = connString[1..];
        string dbName = Regex.Match(ConnectionString, @"(?<=(Database=))[a-zA-Z0-9_\-]+").Value;
        
        using var db = new NPoco.Database(connString, null, MySqlConnector.MySqlConnectorFactory.Instance);
        bool exists = string.IsNullOrEmpty(db.ExecuteScalar<string>("select schema_name from information_schema.schemata where schema_name = @0", dbName)) == false;
        if (exists)
        {
            if(recreate == false)
                return DbCreateResult.AlreadyExisted;
            Logger.Instance.ILog("Dropping existing database");
            db.Execute($"drop database {dbName};");
        }

        Logger.Instance.ILog("Creating Database");
        return db.Execute("create database " + dbName) > 0 ? DbCreateResult.Created : DbCreateResult.Failed;
    }

    protected override void CreateStoredProcedures()
    {
        Logger.Instance.ILog("Creating Stored Procedures");
        using var db = new NPoco.Database(ConnectionString + ";Allow User Variables=True", null, MySqlConnector.MySqlConnectorFactory.Instance);
        
        
        var scripts = GetStoredProcedureScripts("MySql");
        foreach (var script in scripts)
        {
            try
            {
                Logger.Instance.ILog("Creating script: " + script.Key);
                var sql = script.Value.Replace("@", "@@");
                db.Execute(sql);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

    protected override bool CreateDatabaseStructure()
    {
        Logger.Instance.ILog("Creating Database Structure");
        
        // string createDbSql = CreateDbScript.Replace("current_timestamp", "now()");
        // createDbSql = createDbSql.Replace("NOT NULL", "COLLATE utf8_unicode_ci  NOT NULL");
        // createDbSql = createDbSql.Replace("TEXT", "MEDIUMTEXT"); // statistics is too big for TEXT...
        using var db = new NPoco.Database(ConnectionString, null, MySqlConnector.MySqlConnectorFactory.Instance);
        db.Execute(CreateMySqlDbScript);

        db.Execute($"CREATE INDEX idx_DbObject_Type ON {nameof(DbObject)}(Type)");
        db.Execute($"CREATE INDEX idx_DbObject_Name ON {nameof(DbObject)}(Name)");
        return true;
    }

    /// <summary>
    /// Gets the library file status  
    /// </summary>
    /// <returns>the library file status counts</returns>
    public override async Task<IEnumerable<LibraryStatus>> GetLibraryFileOverview()
    {
        int quarter = TimeHelper.GetCurrentQuarter();
        using var db = GetDb();
        string sql = $"call GetLibraryFiles(0, {quarter}, 0, 0, null, 1)";
        var results = (await db.FetchAsync<LibraryStatus>(sql)).ToList();
        return results;
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
        var dbObjects = await db.FetchAsync<DbObject>("call GetLibraryFiles(@0, @1, @2, @3, @4, 0)", (int)status, quarter, start, max, nodeUid);
        return ConvertFromDbObject<LibraryFile>(dbObjects);
    }

    /// <summary>
    /// Gets the shrinkage group data
    /// </summary>
    /// <returns>the shrinkage group data</returns>
    public override async Task<IEnumerable<ShrinkageData>> GetShrinkageGroups()
    {
        using var db = GetDb();
        return await db.FetchAsync<ShrinkageData>("call GetShrinkageData()");
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
            "and JSON_EXTRACT(Data,'$.Type') = @1 " +
            "and JSON_EXTRACT(Data,'$.Enabled') = 1 ",
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
            var builder = new MySqlConnector.MySqlConnectionStringBuilder();
            builder["Server"] = server;
            builder["Uid"] = user;
            builder["Pwd"] = password;
            string connString = builder.ConnectionString;
            using var db = new NPoco.Database(connString, null, MySqlConnector.MySqlConnectorFactory.Instance);
            bool exists = string.IsNullOrEmpty(db.ExecuteScalar<string>("select schema_name from information_schema.schemata where schema_name = @0", name)) == false;
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
        var builder = new MySqlConnector.MySqlConnectionStringBuilder();
        builder["Server"] = server;
        builder["Database"] = database?.EmptyAsNull() ?? "FileFlows";
        builder["Uid"] = user;
        builder["Pwd"] = password;
        string connstr = builder.ConnectionString;
        connstr = connstr.Replace("User ID", "Uid");
        return connstr;
    }

    /// <summary>
    /// Populates the database settings from a connection string
    /// </summary>
    /// <param name="settings">the settings to populate</param>
    /// <param name="connectionString">the connection string to parse</param>
    internal void PopulateSettings(Settings settings, string connectionString)
    {
        var builder = new MySqlConnector.MySqlConnectionStringBuilder(connectionString);
        settings.DbType = DatabaseType.MySql;
        settings.DbServer = builder["Server"].ToString();
        settings.DbName = builder["Database"].ToString();
        settings.DbUser = builder["Uid"].ToString();
        settings.DbPassword = builder["Pwd"].ToString();
    }
}