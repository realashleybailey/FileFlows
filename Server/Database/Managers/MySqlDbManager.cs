using System.Numerics;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
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
        );

        CREATE TABLE {nameof(DbLogMessage)}(
            ClientUid       VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL,
            LogDate         datetime           default           now(),
            Type            int                NOT NULL,            
            Message         TEXT               COLLATE utf8_unicode_ci      NOT NULL
        );

";
    /// <summary>
    /// Creates an instance of a MySqlDbManager
    /// </summary>
    /// <param name="connectionString">a mysql connection string</param>
    public MySqlDbManager(string connectionString)
    {
        ConnectionString = connectionString;
    }
    
    protected override NPoco.Database GetDb()
    {
        return new NPoco.Database(ConnectionString,
            null,
            MySqlConnector.MySqlConnectorFactory.Instance);
    }
    
    private string GetDatabaseName(string connectionString)
        => Regex.Match(ConnectionString, @"(?<=(Database=))[a-zA-Z0-9_\-]+").Value;

    protected override DbCreateResult CreateDatabase(bool recreate)
    {
        string connString = Regex.Replace(ConnectionString, "(^|;)Database=[^;]+", "");
        if (connString.StartsWith(";"))
            connString = connString[1..];
        string dbName = GetDatabaseName(ConnectionString);
        
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
        return db.Execute("create database " + dbName + " character set utf8 collate 'utf8_unicode_ci';") > 0 ? DbCreateResult.Created : DbCreateResult.Failed;
    }

    protected override void CreateStoredProcedures()
    {
        Logger.Instance.ILog("Adding virtual columns");
        AddVirtualColumns();
        
        Logger.Instance.ILog("Creating Stored Procedures");
        using var db = new NPoco.Database(ConnectionString + ";Allow User Variables=True", null, MySqlConnector.MySqlConnectorFactory.Instance);
        
        
        var scripts = GetStoredProcedureScripts("MySql");
        foreach (var script in scripts)
        {
            Logger.Instance.ILog("Creating script: " + script.Key);
            var sql = script.Value.Replace("@", "@@");
            db.Execute(sql);
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
    /// Adds virtual columns to database to improve performance
    /// </summary>
    public void AddVirtualColumns()
    {
        using var db = GetDb();
        string dbName = GetDatabaseName(ConnectionString);
        var existingColumns = db.Fetch<string>($"SELECT COLUMN_NAME FROM information_schema.COLUMNS where TABLE_NAME = '{nameof(DbObject)}' and TABLE_SCHEMA = '{dbName}';");
        
        var columns = new []{
            new []{"js_Status", "ADD COLUMN js_Status int GENERATED ALWAYS AS (json_extract(Data,'$.Status')) VIRTUAL"},
            new []{"js_Order", "ADD COLUMN js_Order int GENERATED ALWAYS AS (json_extract(Data,'$.Order')) VIRTUAL"},
            new []{"js_OriginalSize", "ADD COLUMN js_OriginalSize bigint GENERATED ALWAYS AS (convert(json_extract(Data,'$.OriginalSize'), signed)) VIRTUAL"},
            new []{"js_ProcessingStarted", "ADD COLUMN js_ProcessingStarted datetime GENERATED ALWAYS AS (convert(substring(JSON_UNQUOTE(JSON_EXTRACT(Data, '$.ProcessingStarted')), 1, 23), datetime)) VIRTUAL"},
            new []{"js_ProcessingEnded", "ADD COLUMN js_ProcessingEnded datetime GENERATED ALWAYS AS (convert(substring(JSON_UNQUOTE(JSON_EXTRACT(Data, '$.ProcessingEnded')), 1, 23), datetime)) VIRTUAL"},
            new []{"js_LibraryUid", "ADD COLUMN js_LibraryUid varchar(36) COLLATE utf8_unicode_ci GENERATED ALWAYS AS (JSON_UNQUOTE(json_extract(Data,'$.Library.Uid'))) VIRTUAL"},
            new []{"js_Enabled", "ADD COLUMN js_Enabled boolean GENERATED ALWAYS AS (convert(json_extract(Data,'$.Enabled'), signed)) VIRTUAL"},
            new []{"js_Priority", "ADD COLUMN js_Priority int GENERATED ALWAYS AS (convert(JSON_UNQUOTE(json_extract(Data,'$.Priority')), signed)) VIRTUAL"},
            new []{"js_ProcessingOrder", "ADD COLUMN js_ProcessingOrder int GENERATED ALWAYS AS (convert(JSON_UNQUOTE(json_extract(Data,'$.ProcessingOrder')), signed)) VIRTUAL"},
            new []{"js_Schedule", "ADD COLUMN js_Schedule varchar(36) COLLATE utf8_unicode_ci GENERATED ALWAYS AS (JSON_UNQUOTE(json_extract(Data,'$.Schedule'))) VIRTUAL"}
        };

        string sql = "";
        foreach (var column in columns)
        {
            string colName = column[0];
            if (existingColumns.Contains(colName))
                continue;
            sql += column[1] + ",\n";
        }

        if (string.IsNullOrEmpty(sql))
        {
            Logger.Instance.ILog("Virtual columns already found in database");
            return;
        }

        sql = sql[..^2]; // remove last ,\n
        
        Logger.Instance.ILog("Adding virtual columns to database");

        sql = "ALTER TABLE DbObject \n" + sql + ";";
        db.Execute(sql);
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
        db.OneTimeCommandTimeout = 120;
        try
        {
            var dbObjects = await db.FetchAsync<DbObject>("call GetLibraryFiles(@0, @1, @2, @3, @4, 0)", (int)status,
                quarter, start, max, nodeUid);
            return ConvertFromDbObject<LibraryFile>(dbObjects);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error getting files from mysql: " + ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Performance a search for library files
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>a list of matching library files</returns>
    public async override Task<IEnumerable<LibraryFile>> SearchLibraryFiles(LibraryFileSearchModel filter)
    {
        if (filter.Limit <= 0 || filter.Limit > 10_000)
            filter.Limit = 1000;
        using var db = GetDb();
        string sql = $"select * from {nameof(DbObject)} ";
        sql += " where Type = 'FileFlows.Shared.Models.LibraryFile'";
        sql += " and (DateCreated between @0 and @1) ";
        if (string.IsNullOrWhiteSpace(filter.Path) == false)
            sql += " and Name like @2 ";
        if (string.IsNullOrWhiteSpace(filter.LibraryName) == false)
            sql += " and JSON_EXTRACT(Data, '$.Library.Name') like @3 ";
        sql += $" limit {filter.Limit};" ;
        var from = filter.FromDate;
        var to = filter.ToDate < new DateTime(2000, 1, 1) ? DateTime.MaxValue : filter.ToDate;
        
        var dbObjects = await db.FetchAsync<DbObject>(sql, from, to,
            string.IsNullOrEmpty(filter.Path) ? string.Empty : "%" + filter.Path + "%",
            string.IsNullOrEmpty(filter.LibraryName) ? string.Empty : "%" + filter.LibraryName + "%");
        return ConvertFromDbObject<LibraryFile>(dbObjects);
        
    }
    /// <summary>
    /// Deletes all the library files from the specified libraries
    /// </summary>
    /// <param name="libraryUids">the UIDs of the libraries</param>
    /// <returns>the task to await</returns>
    public override async Task DeleteLibraryFilesFromLibraries(Guid[] libraryUids)
    {
        string sql = "";
        foreach (var uid in libraryUids ?? new Guid[] { })
            sql += $"delete from {nameof(DbObject)} where js_LibraryUid = '{uid}';\n";
        if (sql == "")
            return;
        using var db = GetDb();
        await db.ExecuteAsync(sql);

    }


    /// <summary>
    /// Gets the processing time for each library file 
    /// </summary>
    /// <returns>the processing time for each library file</returns>
    public override async Task<IEnumerable<LibraryFileProcessingTime>> GetLibraryProcessingTimes()
    {
        using var db = GetDb();
        return await db.FetchAsync<LibraryFileProcessingTime>(@"SELECT 
        JSON_UNQUOTE(JSON_EXTRACT(DATA, '$.Library.Name')) AS Library,
        js_OriginalSize as OriginalSize,
        timestampdiff(second, js_ProcessingStarted, js_ProcessingEnded) AS Seconds
        from FileFlowsDocker.DbObject where TYPE = 'FileFlows.Shared.Models.LibraryFile'
        AND js_Status = 1 AND js_ProcessingEnded > js_ProcessingStarted;"); 
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
    /// Logs a message to the database
    /// </summary>
    /// <param name="clientUid">The UID of the client, use Guid.Empty for the server</param>
    /// <param name="type">the type of log message</param>
    /// <param name="message">the message to log</param>
    public override async Task Log(Guid clientUid, LogType type, string message)
    {
        using var db = GetDb();
        await db.ExecuteAsync(
            $"insert into {nameof(DbLogMessage)} ({nameof(DbLogMessage.ClientUid)}, {nameof(DbLogMessage.Type)}, {nameof(DbLogMessage.Message)}) " +
            $" values (@0, @1, @2)", clientUid, type, message);
    }

    /// <summary>
    /// Prune old logs from the database
    /// </summary>
    /// <param name="maxLogs">the maximum number of log messages to keep</param>
    public override async Task PruneOldLogs(int maxLogs)
    {
        try
        {
            using var db = GetDb();
            await db.ExecuteAsync($"call DeleteOldLogs({maxLogs});");
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Searches the log using the given filter
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the messages found in the log</returns>
    public override async Task<IEnumerable<DbLogMessage>> SearchLog(LogSearchModel filter)
    {
        using var db = GetDb();
        string clientUid = filter.ClientUid?.ToString()?.EmptyAsNull() ?? Guid.Empty.ToString();
        string from = filter.FromDate.ToString("yyyy-MM-dd HH:mm:ss");
        string to = filter.ToDate.ToString("yyyy-MM-dd HH:mm:ss");
        string sql = $"select * from {nameof(DbLogMessage)} " +
                     $"where ({nameof(DbLogMessage.LogDate)} between '{from}' and '{to}') " +
                     $" and {nameof(DbLogMessage.ClientUid)} = '{clientUid}' " +
                     (filter.Type != null ?
                         $" and {nameof(DbLogMessage.Type)} {(filter.TypeIncludeHigherSeverity ? "<=" : "=")} {(int)filter.Type}" 
                     : "") +
                     (filter.Message?.EmptyAsNull() != null ? $" and {nameof(DbLogMessage.Message)} like @2 " : "") +
                     $" order by {nameof(DbLogMessage.LogDate)} desc " +
                     " limit 1000";
        var results = await db.FetchAsync<DbLogMessage>(sql, filter.FromDate, filter.ToDate,
            string.IsNullOrWhiteSpace(filter.Message) ? string.Empty : "%" + filter.Message.Trim() + "%");
        // need to reverse them as they're ordered newest at top
        results.Reverse();
        return results;
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
    internal void PopulateSettings(SettingsUiModel settings, string connectionString)
    {
        var builder = new MySqlConnector.MySqlConnectionStringBuilder(connectionString);
        settings.DbType = DatabaseType.MySql;
        settings.DbServer = builder["Server"].ToString();
        settings.DbName = builder["Database"].ToString();
        settings.DbUser = builder["Uid"].ToString();
        settings.DbPassword = builder["Pwd"].ToString();
    }
}