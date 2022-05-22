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
        
        using var db = new NPoco.Database(connString, null, MySqlConnector.MySqlConnectorFactory.Instance);
        bool exists = string.IsNullOrEmpty(db.ExecuteScalar<string>("select schema_name from information_schema.schemata where schema_name = 'FileFlows'")) == false;
        if (exists)
            return DbCreateResult.AlreadyExisted;
        
        return db.Execute("create database FileFlows") > 0 ? DbCreateResult.Created : DbCreateResult.Failed;
    }

    protected override bool CreateDatabaseStructure()
    {
        string createDbSql = CreateDbScript.Replace("current_timestamp", "now()");
        using var db = new NPoco.Database(ConnectionString, null, MySqlConnector.MySqlConnectorFactory.Instance);
        db.Execute(createDbSql);
        return true;
    }
}