using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v1.0.2
/// </summary>
public class Upgrade_1_0_2
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 1.0.2 upgrade script");
        MigrateLibraryFilesData();
    }

    private void MigrateLibraryFilesData()
    {
        Logger.Instance.ILog("Migrating Library Files Data");
        var manager = DbHelper.GetDbManager();

        if (manager is MySqlDbManager)
        {
            DropVirtualColumns(manager);
            DropOldStoredProcs(manager);
        }
        
        CreateLibraryFilesTable(manager);
        
        var files = DbHelper.Select<LibraryFile>().Result.ToArray();
        Logger.Instance.ILog($"About to migrate {files.Length} file{(files.Length == 1 ? "" : "s")}");
        
        using (var db = manager.GetDb().Result)
        {
            db.Db.Mappers.Add(new CustomDbMapper());
            foreach (var file in files)
            {
                db.Db.Insert(file);
            }

            db.Db.Execute("delete from DbObject where Type = @0", typeof(LibraryFile).FullName);
        }
    }

    private void DropOldStoredProcs(DbManager manager)
    {
        using (var db = manager.GetDb().Result)
        {
            db.Db.Execute("DROP PROCEDURE IF EXISTS GetLibraryFiles");
            db.Db.Execute("DROP PROCEDURE IF EXISTS GetShrinkageData");
            db.Db.Execute("DROP PROCEDURE IF EXISTS SearchLibraryFiles");
        }
    }

    private void DropVirtualColumns(DbManager manager)
    {
        using (var db = manager.GetDb().Result)
        {
            db.Db.Execute("alter table DbObject drop column js_Status");
            db.Db.Execute("alter table DbObject drop column js_Order");
            db.Db.Execute("alter table DbObject drop column js_OriginalSize");
            db.Db.Execute("alter table DbObject drop column js_ProcessingStarted");
            db.Db.Execute("alter table DbObject drop column js_ProcessingEnded");
            db.Db.Execute("alter table DbObject drop column js_LibraryUid");
            db.Db.Execute("alter table DbObject drop column js_Enabled");
            db.Db.Execute("alter table DbObject drop column js_Priority");
            db.Db.Execute("alter table DbObject drop column js_ProcessingOrder");
            db.Db.Execute("alter table DbObject drop column js_Schedule");
        }
    }

    private void CreateLibraryFilesTable(DbManager manager)
    {
        string sql = SqlHelper.CleanSql(@"
CREATE TABLE IF NOT EXISTS LibraryFile
(
    -- common fields from DbObject
    Uid                 VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL          PRIMARY KEY,
    Name                VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
    DateCreated         datetime           default           now()      NOT NULL,
    DateModified        datetime           default           now()      NOT NULL,
    
    -- properties
    RelativePath        VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
    Status              int                NOT NULL,
    ProcessingOrder     int                NOT NULL,
    Fingerprint         VARCHAR(255)       COLLATE utf8_unicode_ci      NOT NULL,
    IsDirectory         boolean            not null,
    
    -- size
    OriginalSize        bigint             NOT NULL,
    FinalSize           bigint             NOT NULL,
    
    -- dates 
    CreationTime        datetime           default           now()      NOT NULL,
    LastWriteTime       datetime           default           now()      NOT NULL,
    HoldUntil           datetime           default           '1970-01-01 00:00:01'      NOT NULL,
    ProcessingStarted   datetime           default           now()      NOT NULL,
    ProcessingEnded     datetime           default           now()      NOT NULL,
    
    -- references
    LibraryUid          varchar(36)        COLLATE utf8_unicode_ci      NOT NULL,
    LibraryName         VARCHAR(100)       COLLATE utf8_unicode_ci      NOT NULL,
    FlowUid             varchar(36)        COLLATE utf8_unicode_ci      NOT NULL,
    FlowName            VARCHAR(100)       COLLATE utf8_unicode_ci      NOT NULL,
    DuplicateUid        varchar(36)        COLLATE utf8_unicode_ci      NOT NULL,
    DuplicateName       VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
    NodeUid             varchar(36)        COLLATE utf8_unicode_ci      NOT NULL,
    NodeName            VARCHAR(100)       COLLATE utf8_unicode_ci      NOT NULL,
    WorkerUid           varchar(36)        COLLATE utf8_unicode_ci      NOT NULL,

    -- output
    OutputPath          VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
    NoLongerExistsAfterProcessing          boolean                      not null,

    -- json data
    OriginalMetadata    TEXT               COLLATE utf8_unicode_ci      NOT NULL,
    FinalMetadata       TEXT               COLLATE utf8_unicode_ci      NOT NULL,
    ExecutedNodes       TEXT               COLLATE utf8_unicode_ci      NOT NULL
);");

        if (DbHelper.UseMemoryCache)
        {
            // sqlite
            sql = sql.Replace("COLLATE utf8_unicode_ci      ", string.Empty);
            sql = sql.Replace("datetime           default           now()", string.Empty);
        }
        
        manager.Execute(sql, null).Wait();

        // create indexes 
        if (DbHelper.UseMemoryCache)
        {
            // sqlite
            manager.Execute("CREATE INDEX IF NOT EXISTS idx_LibraryFile_Status ON LibraryFile (Status)", null).Wait();
            manager.Execute("CREATE INDEX IF NOT EXISTS idx_LibraryFile_DateModified ON LibraryFile (DateModified)", null).Wait();
            manager.Execute("CREATE INDEX IF NOT EXISTS idx_LibraryFile_StatusHoldLibrary ON LibraryFile (Status, HoldUntil, LibraryUid)", null).Wait();
        }
        else
        {
            // mysql 
            manager.Execute("ALTER TABLE LibraryFile ADD INDEX (Status)", null).Wait();
            manager.Execute("ALTER TABLE LibraryFile ADD INDEX (DateModified)", null).Wait();
            manager.Execute("ALTER TABLE LibraryFile ADD INDEX (Status, HoldUntil, LibraryUid)", null).Wait();
        }
    }

}