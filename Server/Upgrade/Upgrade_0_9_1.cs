using System.Text.RegularExpressions;
using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v0.9.1
/// </summary>
public class Upgrade_0_9_1
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.9.1 upgrade script");
        AddNameIndexToMySql();
    }
    /// <summary>
    /// Adds indexes to the log message table
    /// </summary>
    private void AddNameIndexToMySql()
    {
        if(DbHelper.GetDbManager() is MySqlDbManager mysql)
            mysql.Execute($"ALTER TABLE {nameof(DbObject)} ADD FULLTEXT NameIndex(Name);", null).Wait();
    }
}