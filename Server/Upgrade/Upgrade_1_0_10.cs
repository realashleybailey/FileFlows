using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v1.0.10
/// </summary>
public class Upgrade_1_0_10
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 1.0.10 upgrade script");
        AddFinalFingerPrintField();
    }

    private void AddFinalFingerPrintField()
    {
        var manager = DbHelper.GetDbManager();
        if (manager.ColumnExists("LibraryFile", "FinalFingerprint").Result)
            return;

        string sql = "ALTER TABLE LibraryFile " +
                     " ADD FinalFingerprint               VARCHAR(255) ";
        if (manager is MySqlDbManager)
            sql += " COLLATE utf8_unicode_ci ";
        sql += " NOT NULL    DEFAULT('')".Trim();
        
        manager.Execute(sql, null).Wait();
    }
}