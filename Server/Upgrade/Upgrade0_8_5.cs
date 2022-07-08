using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v0.8.5
/// </summary>
public class Upgrade0_8_5
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.8.4 upgrade script");
        AddStatisticsTable();
    }


    /// <summary>
    /// Adds indexes to the log message table
    /// </summary>
    private void AddStatisticsTable()
    {
        if(DbHelper.GetDbManager() is SqliteDbManager sqlite)
            sqlite.Execute(sqlite.CreateDbStatisticTableScript, new object[]{});
    }
}