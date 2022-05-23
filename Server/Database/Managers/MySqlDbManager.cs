using System.Text.RegularExpressions;
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
}