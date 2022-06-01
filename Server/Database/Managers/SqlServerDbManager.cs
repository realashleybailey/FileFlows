using System.Data.SqlClient;
using System.Text.RegularExpressions;
using FileFlows.Shared.Models;
using NPoco;

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

    protected override DbCreateResult CreateDatabase()
    {
        string connString = Regex.Replace(ConnectionString, "(^|;)Database=[^;]+", "");
        if (connString.StartsWith(";"))
            connString = connString[1..];
        string dbName = Regex.Match(ConnectionString, @"(?<=(Database=))[a-zA-Z0-9_\-]+").Value;
        
        using var db = new NPoco.Database(connString, null, SqlClientFactory.Instance);
        bool exists = string.IsNullOrEmpty(db.ExecuteScalar<string>("select name from sys.databases where name = @0", dbName)) == false;
        if (exists)
            return DbCreateResult.AlreadyExisted;

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
}