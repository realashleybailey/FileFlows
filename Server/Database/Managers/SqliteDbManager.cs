using System.Data.SQLite;
using System.Text.RegularExpressions;
using FileFlows.Shared.Models;
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
    protected override IDatabase GetDb() => GetDb(this.ConnectionString);

    /// <summary>
    /// Gets a database instance
    /// </summary>
    /// <param name="connectionString">the connection of the database to open</param>
    /// <returns>a database instance</returns>
    internal static NPoco.Database GetDb(string connectionString)
    {
        try
        {
            var db = new NPoco.Database(connectionString, null, SQLiteFactory.Instance);
            db.Mappers.Add(new UidConverter());
            return db;
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
    /// <param name="recreate">if the database should be recreated if already exists</param>
    /// <returns>true if successfully created</returns>
    protected override DbCreateResult CreateDatabase(bool recreate)
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

    public override Task<LibraryFileStatusOverview> GetLibraryFileOverview()
    {
        throw new NotImplementedException();
    }

    public override Task<IEnumerable<LibraryFile>> GetLibraryFiles(FileStatus status)
    {
        throw new NotImplementedException();
    }

    public override Task<Flow> GetFailureFlow(Guid libraryUid)
    {
        throw new NotImplementedException();
    }

    #endregion
}