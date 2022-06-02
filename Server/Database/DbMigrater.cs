using System.Data.SQLite;
using FileFlows.Server.Database.Managers;

namespace FileFlows.Server.Database;

/// <summary>
/// Migrates one database to another database
/// </summary>
public class DbMigrater
{
    /// <summary>
    /// Migrates data from one database to another
    /// </summary>
    /// <param name="sourceConnection">the connection string of the source database</param>
    /// <param name="destinationConnection">the connection string of the destination database</param>
    /// <returns>if the migration was successful</returns>
    public static bool Migrate(string sourceConnection, string destinationConnection)
    {
        Logger.Instance?.ILog("Database Migration started");

        using var source = GetDatabase(sourceConnection);

        var destDbManager = DbManager.GetManager(destinationConnection);
        destDbManager.CreateDb(insertInitialData: false).Wait();
        using var dest = GetDatabase(destinationConnection);

        var dbObjects = source.Fetch<DbObject>($"select * from {nameof(DbObject)}")?.ToArray();
        if (dbObjects?.Any() != true)
        {
            Logger.Instance?.ILog("Database Migration finished with nothing to migrate");
            return true;
        }

        foreach (var obj in dbObjects)
        {
            try
            {
                Logger.Instance?.DLog($"Migrating [{obj.Uid}][{obj.Type}]: {obj.Name ?? string.Empty}");

                dest.Execute(
                    $"insert into {nameof(DbObject)} (Uid, Name, Type, DateCreated, DateModified, Data) values (@0, @1, @2, @3, @4, @5)",
                    obj.Uid.ToString(),
                    obj.Name,
                    obj.Type,
                    obj.DateCreated,
                    obj.DateModified,
                    obj.Data ?? string.Empty);
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Failed to migrate data: " + ex.Message);
                return false;
            }
        }

        Logger.Instance?.ILog("Database Migration complete");

        return true;
    }

    /// <summary>
    /// Gets a Database from its connection string
    /// </summary>
    /// <param name="connectionString">the connection string</param>
    /// <returns></returns>
    private static NPoco.Database GetDatabase(string connectionString)
    {
        if(connectionString.Contains(".sqlite"))
            return SqliteDbManager.GetDb(connectionString);
        
        if(connectionString.Contains(";Uid="))
            return new NPoco.Database(connectionString,
                null,
                MySqlConnector.MySqlConnectorFactory.Instance);
        
        return new NPoco.Database(connectionString, null, System.Data.SqlClient.SqlClientFactory.Instance);
    }
}