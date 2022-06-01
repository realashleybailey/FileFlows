using System.Text.RegularExpressions;
using FileFlows.Shared;
using NPoco;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// A database manager used to communicate with a MySql/MariaDB Database
/// </summary>
public class MySqlDbManager: DbManager
{
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

    protected override DbCreateResult CreateDatabase()
    {
        string connString = Regex.Replace(ConnectionString, "(^|;)Database=[^;]+", "");
        if (connString.StartsWith(";"))
            connString = connString[1..];
        string dbName = Regex.Match(ConnectionString, @"(?<=(Database=))[a-zA-Z0-9_\-]+").Value;
        
        using var db = new NPoco.Database(connString, null, MySqlConnector.MySqlConnectorFactory.Instance);
        bool exists = string.IsNullOrEmpty(db.ExecuteScalar<string>("select schema_name from information_schema.schemata where schema_name = @0", dbName)) == false;
        if (exists)
            return DbCreateResult.AlreadyExisted;

        Logger.Instance.ILog("Creating Database");
        return db.Execute("create database " + dbName) > 0 ? DbCreateResult.Created : DbCreateResult.Failed;
    }

    protected override void CreateStoredProcedures()
    {
        Logger.Instance.ILog("Creating Stored Procedures");
        using var db = new NPoco.Database(ConnectionString, null, MySqlConnector.MySqlConnectorFactory.Instance);
        string sqlGetNextLibraryFile = GetSqlScript("MySql", "GetNextLibraryFile.sql");
        if (string.IsNullOrEmpty(sqlGetNextLibraryFile) == false)
        {
            try
            {
                sqlGetNextLibraryFile = sqlGetNextLibraryFile.Replace("@", "@@");
                db.Execute(sqlGetNextLibraryFile);
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

        string createDbSql = CreateDbScript.Replace("current_timestamp", "now()")
            .Replace("TEXT", "MEDIUMTEXT"); // statistics is too big for TEXT...
        using var db = new NPoco.Database(ConnectionString, null, MySqlConnector.MySqlConnectorFactory.Instance);
        db.Execute(createDbSql);

        db.Execute("ALTER TABLE DbObject ADD INDEX (Type, Name);");
        return true;
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
        return builder.ConnectionString;
    }
}