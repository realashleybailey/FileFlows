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
        string dbName = builder["Database"].ToString();
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
        string sqlGetNextLibraryFile = GetSqlScript("SqlServer", "GetNextLibraryFile.sql");
        if (string.IsNullOrEmpty(sqlGetNextLibraryFile) == false)
        {
            try
            {
                foreach (string script in sqlGetNextLibraryFile.Split(new string[] { "\nGO" },
                             StringSplitOptions.RemoveEmptyEntries))
                {
                    var sqlScript = script.Replace("@", "@@").Trim();
                    db.Execute(sqlScript);
                }
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

        string createDbSql = CreateDbScript.Replace("current_timestamp", "getdate()")
            .Replace("VARCHAR(36)", "uniqueidentifier")
            .Replace("TEXT", "varchar(max)");
        using var db = new NPoco.Database(ConnectionString, null, SqlClientFactory.Instance);
        db.Execute(createDbSql);

        db.Execute("ALTER TABLE DbObject ADD INDEX (Type, Name);");
        return true;
    }


    /// <summary>
    /// Gets the next library file to process
    /// </summary>
    /// <param name="node">the node executing this library file</param>
    /// <param name="workerUid">the UID of the worker</param>
    /// <returns>the next library file to process</returns>
    public override async Task<LibraryFile> GetNextLibraryFile(ProcessingNode node, Guid workerUid)
    {
        int quarter = TimeHelper.GetCurrentQuarter();
        using var db = GetDb();

        try
        {

            var result = await db.FirstOrDefaultAsync<LibraryFile>("exec GetNextLibraryFile @@NodeUid=@0, @@WorkerUid=@1, @@IntervalIndex=@2, @@StartDate=@3", 
                node.Uid, workerUid, quarter, DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
            return result;
        }
        catch(Exception ex)
        {
            throw;
        }
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
    internal void PopulateSettings(Settings settings, string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        settings.DbType = DatabaseType.SqlServer;
        settings.DbServer = builder["Server"].ToString();
        settings.DbName = builder["Database"].ToString();
        settings.DbUser = builder["User"].ToString();
        settings.DbPassword = builder["Password"].ToString();
    }
}