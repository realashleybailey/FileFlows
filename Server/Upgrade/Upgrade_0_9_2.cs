using System.Text.RegularExpressions;
using FileFlows.Server.Controllers;
using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v0.9.2
/// </summary>
public class Upgrade_0_9_2
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.9.2 upgrade script");
        AddRevisionedObjectTable();
        #if(!DEBUG)
        AddRevisions();
        #endif
    }
    /// <summary>
    /// Adds the revisioned object database table
    /// </summary>
    private void AddRevisionedObjectTable()
    {
        if(DbHelper.GetDbManager() is MySqlDbManager mysql)
            mysql.Execute(mysql.CreateDbRevisionedObjectTableScript, null);
        else if(DbHelper.GetDbManager() is SqliteDbManager sqlite)
            sqlite.Execute(sqlite.CreateDbRevisionedObjectTableScript, null);
    }

    private void AddRevisions()
    {
        var manager = DbHelper.GetDbManager();
        foreach (string type in new string[] { nameof(Library), nameof(Flow), nameof(PluginSettingsModel) })
        {
            var dbObjects = manager
                .Fetch<DbObject>($"select * from DbObject where Type = 'FileFlows.Shared.Models.{type}'").Result;
            foreach (var dbo in dbObjects)
            {
                RevisionController.SaveRevision(dbo).Wait();
            }
        }
    }
}