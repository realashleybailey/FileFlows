using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using Microsoft.Extensions.Localization;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v1.0.5
/// </summary>
public class Upgrade_1_0_5
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 1.0.5 upgrade script");
        RemoveDuplicateFiles();
    }

    private void RemoveDuplicateFiles()
    {
        var manager = DbHelper.GetDbManager();

        if (manager is MySqlDbManager mysql)
        {
            // delete any duplicates
            manager.Execute(@"DELETE c1 FROM LibraryFile c1
        INNER JOIN LibraryFile c2 
        WHERE
        c1.DateCreated > c2.DateCreated AND 
        c1.Name = c2.Name", null).Wait();
            mysql.Execute("ALTER TABLE LibraryFile ADD UNIQUE (`Name`)", null).Wait();
        }
        else
        {
            // sqlite
            // delete any duplicates
            manager.Execute(@"DELETE FROM LibraryFile WHERE rowid NOT IN (SELECT min(rowid) FROM LibraryFile GROUP BY Name)", null).Wait();
            manager.Execute("CREATE UNIQUE INDEX LibraryFileUniqueName ON LibraryFile(Name)", null).Wait();
        }
    }
}