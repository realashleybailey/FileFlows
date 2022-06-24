using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v0.8.3
/// </summary>
public class Upgrade0_8_3
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.8.3 upgrade script");
        DeleteStatistics();
    }

    /// <summary>
    /// Delete the old statistics from the database
    /// In 0.8.3 we introduced a new statistic table with better statistics 
    /// </summary>
    private void DeleteStatistics()
    {
        int rows = DbHelper.Execute($"delete from {nameof(DbObject)} where Type = 'FileFlows.Shared.Models.Statistics'").Result;
        if(rows > 0)
            Logger.Instance.ILog("Deleted old statistics from database");
    }
}