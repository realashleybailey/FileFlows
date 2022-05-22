using System.Data.SQLite;
using System.Text.RegularExpressions;
using NPoco;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// A database manager used to communicate with a Sqlite Database
/// </summary>
public class SqliteDbManager : DbManager
{
    private readonly string DbFilename;
    
    /// <summary>
    /// Constructs a new Sqlite Database Manager
    /// </summary>
    /// <param name="connectionString">The connection string to the database</param>
    public SqliteDbManager(string connectionString)
    {
        // Data Source={DbFilename};Version=3;
        ConnectionString = connectionString;
        DbFilename = Regex.Match(connectionString, @"(?<=(Data Source=))[^;]+").Value;
    }

    /// <summary>
    /// Constructs a new Sqlite Database Manager for a sqlite db file
    /// </summary>
    /// <param name="dbFile">the filename of the sqlite db file</param>
    /// <returns>A new Sqlite Database Manager instance</returns>
    public static SqliteDbManager ForFile(string dbFile) => new SqliteDbManager(GetConnetionString(dbFile));

    /// <summary>
    /// Gets a sqlite connection string for a db file
    /// </summary>
    /// <param name="dbFile">the filename of the sqlite db file</param>
    /// <returns>a sqlite connection string</returns>
    public static string GetConnetionString(string dbFile) => $"Data Source={dbFile};Version=3";

    /// <summary>
    /// Gets if the database manager should use a memory cache
    /// Sqlite uses a memory cache due to its limitation of concurrent reading/writing
    /// </summary>
    public override bool UseMemoryCache => true;

    /// <summary>
    /// Get an instance of the IDatabase
    /// </summary>
    /// <returns>an instance of the IDatabase</returns>
    protected override IDatabase GetDb()
    {
        try
        {
            return new NPoco.Database(ConnectionString, null, SQLiteFactory.Instance);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error loading database: " + ex.Message);
            throw;
        }
    }

    #region setup code
    /// <summary>
    /// Creates the actual Database
    /// </summary>
    /// <returns>true if successfully created</returns>
    protected override DbCreateResult CreateDatabase()
    {
        if (File.Exists(DbFilename) == false)
        {
            SQLiteConnection.CreateFile(DbFilename);
            return DbCreateResult.Created;
        }
        
        // create backup 
        File.Copy(DbFilename, DbFilename + ".backup", true);
        return DbCreateResult.AlreadyExisted;
    }

    /// <summary>
    /// Creates the tables etc in the database
    /// </summary>
    /// <returns>true if successfully created</returns>
    protected override bool CreateDatabaseStructure()
    {
        using var con = new SQLiteConnection($"Data Source={DbFilename};Version=3;");
        con.Open();
        try
        {
            using var cmdExists =
                new SQLiteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name='{nameof(DbObject)}'",
                    con);
            if (cmdExists.ExecuteScalar() != null)
                return true; // tables exist, all good

            using var cmd = new SQLiteCommand(CreateDbScript, con);
            cmd.ExecuteNonQuery();
        }
        finally
        {
            con.Close();
        }
            

        return true;// tables exist, all good
    }
    #endregion
}