using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v0.8.4
/// </summary>
public class Upgrade0_8_4
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.8.4 upgrade script");
        AddLogMessageIndexes();
    }


    /// <summary>
    /// Adds indexes to the log message table
    /// </summary>
    private void AddLogMessageIndexes()
    {
        if (DbHelper.UseMemoryCache)
            return; // not using external database
        var manager = DbHelper.GetDbManager();
        manager.Execute($"CREATE INDEX idx_{nameof(DbLogMessage)}_Client ON {nameof(DbLogMessage)}(ClientUid);", null);
        manager.Execute($"CREATE INDEX idx_{nameof(DbLogMessage)}_LogDate ON {nameof(DbLogMessage)}(LogDate);", null);
    }
}