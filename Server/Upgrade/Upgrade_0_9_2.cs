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
    internal readonly string MySqlCreateDbRevisionedObjectTableScript = @$"
        CREATE TABLE {nameof(RevisionedObject)}(
            Uid             VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL          PRIMARY KEY,
            RevisionUid     VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL,
            RevisionName    VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
            RevisionType    VARCHAR(255)       COLLATE utf8_unicode_ci      NOT NULL,
            RevisionDate    datetime           default           now(),
            RevisionCreated datetime           default           now(),
            RevisionData    MEDIUMTEXT         COLLATE utf8_unicode_ci      NOT NULL
        );";
    internal readonly string SqliteCreateDbRevisionedObjectTableScript = @$"
        CREATE TABLE {nameof(RevisionedObject)}(
            Uid             VARCHAR(36)        NOT NULL          PRIMARY KEY,
            RevisionUid     VARCHAR(36)        NOT NULL,
            RevisionName    VARCHAR(1024)      NOT NULL,
            RevisionType    VARCHAR(255)       NOT NULL,
            RevisionDate    datetime           default           current_timestamp,
            RevisionCreated datetime           default           current_timestamp,
            RevisionData    TEXT               NOT NULL
        );
";

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
            mysql.Execute(MySqlCreateDbRevisionedObjectTableScript, null).Wait();
        else if(DbHelper.GetDbManager() is SqliteDbManager sqlite)
            sqlite.Execute(SqliteCreateDbRevisionedObjectTableScript, null).Wait();
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