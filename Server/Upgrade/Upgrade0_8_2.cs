using FileFlows.Server.Database.Managers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v0.8.2
/// </summary>
public class Upgrade0_8_2
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.8.2 upgrade script");
        AddVirtualColumnsToDatabase();
    }

    private void AddVirtualColumnsToDatabase()
    {
        var manager = MySqlDbManager.GetManager(AppSettings.Instance.DatabaseConnection);
        if (manager is MySqlDbManager mysql == false)
            return;
        
        // using external db, add virtual columns
        mysql.AddVirtualColumns();
    }
}