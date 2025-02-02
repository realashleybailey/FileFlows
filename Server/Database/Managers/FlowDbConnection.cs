using FileFlows.Plugin;
using FileFlows.Server.Helpers;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// A wrapped database connection that limits the number of open connections
/// </summary>
public class FlowDbConnection:IDisposable
{
    private static int MAX_CONNECTIONS = 30;
    private static SemaphoreSlim semaphore;
    private static bool Initialized = false;

    /// <summary>
    /// Gets the database instance
    /// </summary>
    public NPoco.Database Db { get; private set; }

    /// <summary>
    /// Initializes the FlowDbConnection and the maximum open SQL connections
    /// </summary>
    /// <param name="sqlite">if the database is SQLite</param>
    internal static void Initialize(bool sqlite)
    {
        if (Initialized)
            return;
        MAX_CONNECTIONS = sqlite ? 1 : 30;
        semaphore = new SemaphoreSlim(MAX_CONNECTIONS, MAX_CONNECTIONS);
        Logger.Instance.ILog("Maximum concurrent database connections: " + MAX_CONNECTIONS);
        Initialized = true;
    }

    /// <summary>
    /// Gets the count of open database connections
    /// </summary>
    public static int GetOpenConnections => MAX_CONNECTIONS - semaphore.CurrentCount;

    /// <summary>
    /// Gets a database connection
    /// </summary>
    /// <param name="instanceCreator">action that creates an instance of the database</param>
    /// <returns>the database connection</returns>
    public static async Task<FlowDbConnection> Get(Func<NPoco.Database> instanceCreator)
    {
        DateTime dt = DateTime.Now;
        await semaphore.WaitAsync();
        var timeWaited = DateTime.Now.Subtract(dt);
        if (timeWaited.TotalMilliseconds > 10)
        {
            await FlowDatabase.Logger.Log(LogType.Info, "Time waited for connection: " + timeWaited);
        }
        var db = new FlowDbConnection();
        db.Db = instanceCreator();
        return db;
    }

    public void Dispose()
    {
        if (Db != null)
        {
            Db.Dispose();
            Db = null;
        }

        try
        {
            semaphore.Release();
        }
        catch (Exception)
        {
        }
    }
}