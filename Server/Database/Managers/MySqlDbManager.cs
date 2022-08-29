using System.Numerics;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using MySqlConnector;
using NPoco;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// A database manager used to communicate with a MySql/MariaDB Database
/// </summary>
public class MySqlDbManager: DbManager
{
    internal readonly string CreateDbRevisionedObjectTableScript = @$"
        CREATE TABLE {nameof(RevisionedObject)}(
            Uid             VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL          PRIMARY KEY,
            RevisionUid     VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL,
            RevisionName    VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
            RevisionType    VARCHAR(255)       COLLATE utf8_unicode_ci      NOT NULL,
            RevisionDate    datetime           default           now(),
            RevisionCreated datetime           default           now(),
            RevisionData    MEDIUMTEXT         COLLATE utf8_unicode_ci      NOT NULL
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

    protected override NPoco.Database GetDbInstance()
    {
        return new FlowDatabase(ConnectionString);
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
        
        using var db = new NPoco.Database(ConnectionString, null, MySqlConnector.MySqlConnectorFactory.Instance);
        string sqlTables = GetSqlScript("MySql", "Tables.sql", clean: true);
        Logger.Instance.ILog("SQL Tables:\n" + sqlTables);
        db.Execute(sqlTables);
        
        return true;
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
        
        
        List<DbObject> dbObjects;
        using (var db = await GetDb())
        {
            string sql = $"call SearchLibraryFiles(@0, @1, @2, @3, @4)";
            dbObjects = (await db.Db.FetchAsync<DbObject>(sql, filter.LibraryName ?? string.Empty, filter.Path ?? string.Empty, filter.FromDate, filter.ToDate, filter.Limit)).ToList();
        }

        return ConvertFromDbObject<LibraryFile>(dbObjects);
        
    }


    /// <summary>
    /// Gets the processing time for each library file 
    /// </summary>
    /// <returns>the processing time for each library file</returns>
    public override async Task<IEnumerable<LibraryFileProcessingTime>> GetLibraryProcessingTimes()
    {
        List<LibraryFileProcessingTime> result;
        using (var db = await GetDb())
        {
            result = await db.Db.FetchAsync<LibraryFileProcessingTime>(@"SELECT 
        JSON_UNQUOTE(JSON_EXTRACT(DATA, '$.Library.Name')) AS Library,
        js_OriginalSize as OriginalSize,
        timestampdiff(second, js_ProcessingStarted, js_ProcessingEnded) AS Seconds
        from DbObject where TYPE = 'FileFlows.Shared.Models.LibraryFile'
        AND js_Status = 1 AND js_ProcessingEnded > js_ProcessingStarted;");
        }

        return result;
    }

    /// <summary>
    /// Gets data for a days/hours heatmap.  Where the list is the days, and the dictionary is the hours with the count as the values
    /// </summary>
    /// <returns>heatmap data</returns>
    public override async Task<List<Dictionary<int, int>>> GetHourProcessingTotals()
    {
        List<(int day, int hour, int count)> data;
        string sql = @"SELECT DAYOFWEEK(js_ProcessingStarted) AS day, HOUR(js_ProcessingStarted) as hour, COUNT(Uid) as count
 from DbObject where TYPE = 'FileFlows.Shared.Models.LibraryFile'
AND js_Status = 1 AND js_ProcessingStarted IS not NULL
GROUP BY DAYOFWEEK(js_ProcessingStarted), HOUR(js_ProcessingStarted);";

        using (var db = await GetDb())
        {
            data = (await db.Db.FetchAsync<(int day, int hour, int count)>(sql));
        }

        var days = new List<Dictionary<int, int>>();
        for (int i = 0; i < 7; i++)
        {
            var results = new Dictionary<int, int>();
            for (int j = 0; j < 24; j++)
            {
                // mysql DAYOFWEEK, sun=1, mon=2, sat =7
                // so we use x.day - 1 here to convert sun=0
                int count = data.Where(x => (x.day - 1) == i && x.hour == j).Select(x => x.count).FirstOrDefault();
                results.Add(j, count);
            }

            days.Add(results);
        }

        return days;
    }

    private readonly List<string> LogMessages = new();
    
    /// <summary>
    /// Logs a message to the database
    /// </summary>
    /// <param name="clientUid">The UID of the client, use Guid.Empty for the server</param>
    /// <param name="type">the type of log message</param>
    /// <param name="message">the message to log</param>
    public override async Task Log(Guid clientUid, LogType type, string message)
    {
        // by bucketing this it greatly improves speed
        string? sql = null;
        lock (LogMessages)
        {
            message = MySqlHelper.EscapeString(message);
            LogMessages.Add(
                $"('{clientUid}', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}', {(int)type}, '{message}')");

            if (LogMessages.Count > 20)
            {
                sql =
                    $"insert into {nameof(DbLogMessage)} ({nameof(DbLogMessage.ClientUid)}, {nameof(DbLogMessage.LogDate)}, {nameof(DbLogMessage.Type)}, {nameof(DbLogMessage.Message)}) values "
                    + string.Join("," + Environment.NewLine, LogMessages);
                LogMessages.Clear();
            }
        }
        if(sql != null)
        {
            using (var db = await GetDb())
            {
                await db.Db.ExecuteAsync(sql);
            }
        }
        // using (var db = await GetDb())
        // {
        //     await db.Db.ExecuteAsync(
        //         $"insert into {nameof(DbLogMessage)} ({nameof(DbLogMessage.ClientUid)}, {nameof(DbLogMessage.Type)}, {nameof(DbLogMessage.Message)}) " +
        //         $" values (@0, @1, @2)", clientUid, type, message);
        // }
    }

    /// <summary>
    /// Prune old logs from the database
    /// </summary>
    /// <param name="maxLogs">the maximum number of log messages to keep</param>
    public override async Task PruneOldLogs(int maxLogs)
    {
        try
        {
            using (var db = await GetDb())
            {
                await db.Db.ExecuteAsync($"call DeleteOldLogs({maxLogs});");
            }
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
        if (filter.Source == "DATABASE" || filter.Source == "HTTP")
        {
            // just read those file.. er no
        }
        string clientUid = filter.Source.EmptyAsNull() ?? Guid.Empty.ToString();
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
        List<DbLogMessage> results;
        DateTime dt = DateTime.Now;
        using (var db = await GetDb())
        {
            results = await db.Db.FetchAsync<DbLogMessage>(sql, filter.FromDate, filter.ToDate,
                string.IsNullOrWhiteSpace(filter.Message) ? string.Empty : "%" + filter.Message.Trim() + "%");
        }

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
        DbObject dbObject;
        using (var db = await GetDb())
        {
            string sql = $@"select * from DbObject where Type = '{typeof(Flow).FullName}' " +
                         $@"and JSON_EXTRACT(Data,'$.Type') = {(int)FlowType.Failure} " +
                         "and JSON_EXTRACT(Data,'$.Default') = 1 " +
                         "and JSON_EXTRACT(Data,'$.Enabled') = 1 ";
                
            dbObject = await db.Db.SingleAsync<DbObject>(sql);
        }

        return ConvertFromDbObject<Flow>(dbObject);
    }
    
    /// <summary>
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    public override async Task<IEnumerable<Statistic>> GetStatisticsByName(string name)
    {
        List<DbStatistic> stats;
        using (var db = await GetDb())
        {
            stats = await db.Db.FetchAsync<DbStatistic>("where Name = @0", name);
        }

        var results = new List<Statistic>();
        foreach (var stat in stats)
        {
            if(stat.Type == StatisticType.Number)
                results.Add(new () { Name = stat.Name, Value = stat.NumberValue});
            if(stat.Type == StatisticType.String)
                results.Add(new () { Name = stat.Name, Value = stat.StringValue});
        }

        return results;
    }
    
    /// <summary>
    /// Tests the connection to a database
    /// </summary>
    /// <param name="server">the server address</param>
    /// <param name="name">the database name</param>
    /// <param name="port">the database port</param>
    /// <param name="user">the connecting user</param>
    /// <param name="password">the password to use</param>
    /// <returns>any error or empty string if successful</returns>
    public string Test(string server, string name, int port, string user, string password)
    {
        try
        {
            var builder = new MySqlConnector.MySqlConnectionStringBuilder();
            builder["Server"] = server;
            if (port > 0)
                builder["Port"] = port;
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
    /// <param name="port">the database port</param>
    /// <param name="user">the connecting user</param>
    /// <param name="password">the password for the connection</param>
    /// <returns>the database connection string</returns>
    public string GetConnectionString(string server, string database, int port, string user, string password)
    {
        var builder = new MySqlConnector.MySqlConnectionStringBuilder();
        builder["Server"] = server;
        if (port > 0)
            builder["Port"] = port;
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
        if (builder.ContainsKey("Port") && int.TryParse(builder["Port"].ToString(), out int port))
            settings.DbPort = port;
        else
            settings.DbPort = 3306;
    }

    private readonly Dictionary<Guid, DateTime> NodeLastSeen = new();

    /// <summary>
    /// Updates the last seen of a node
    /// </summary>
    /// <param name="uid">the UID of the node</param>
    public override async Task UpdateNodeLastSeen(Guid uid)
    {
        lock (NodeLastSeen)
        {
            if (NodeLastSeen.ContainsKey(uid))
            {
                 if(NodeLastSeen[uid] > DateTime.Now.AddSeconds(-20))
                     return; // so recent, don't record it
                 NodeLastSeen[uid] = DateTime.Now;
            }
            else
            {
                NodeLastSeen.Add(uid, DateTime.Now);
            }
        }
        

        string dt = DateTime.Now.ToString("o"); // same format as json
        
        using (var db = await GetDb())
        {
            string sql = $"update DbObject set Data = json_set(Data, '$.LastSeen', '{dt}') where Type = 'FileFlows.Shared.Models.ProcessingNode' and Uid = '{uid}'";
            await db.Db.ExecuteAsync(sql);
        }
    }
    
    
    
    /// <summary>
    /// Updates work on a library file
    /// </summary>
    /// <param name="libraryFile">The library file to update</param>
    public override async Task UpdateWork(LibraryFile libraryFile)
    {
        if (libraryFile == null)
            return;
        
        using (var db = await GetDb())
        {
            string sql = "update DbObject set Data = " +
                         "json_set(Data" +
                         $", '$.Status', {(int)libraryFile.Status}" +
                         $", '$.FinalSize', {libraryFile.FinalSize}" +
                         (libraryFile.Node == null ? "" : (
                             $@", '$.Node', JSON_OBJECT('Uid', '{libraryFile.Node.Uid}', 'Name', '{MySqlHelper.EscapeString(libraryFile.Node.Name)}', 'Type', '{typeof(ProcessingNode).FullName}')"
                         )) +
                         $", '$.WorkerUid', '{libraryFile.WorkerUid}'" +
                         $", '$.ExecutedNodes'," + 
                             $"JSON_ARRAY(" +
                                (libraryFile.ExecutedNodes?.Any() == true ? 
                                    string.Join(",", libraryFile.ExecutedNodes.Select(x => 
                                        "JSON_OBJECT(" +
                                        $"'NodeName', '{MySqlHelper.EscapeString(x.NodeName)}', " +
                                        $"'NodeUid', '{x.NodeUid}', " +
                                        $"'ProcessingTime', '{x.ProcessingTime}', " +
                                        $"'Output', {x.Output})")
                                    ) 
                                    : ""
                                ) +
                            ")" +
                         $", '$.ProcessingStarted', '{libraryFile.ProcessingStarted.ToString("o")}'" +
                         $", '$.ProcessingEnded', '{libraryFile.ProcessingEnded.ToString("o")}'" +
                         $") where Type = 'FileFlows.Shared.Models.LibraryFile' and Uid = '{libraryFile.Uid}'";
            await db.Db.ExecuteAsync(sql);
        }
        
    }
}