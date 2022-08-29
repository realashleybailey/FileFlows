using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using NPoco;

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
    Uid                 VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL          PRIMARY KEY       DEFAULT(''),
    Name                VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL          DEFAULT(''),
    DateCreated         datetime           default           now(),
    DateModified        datetime           default           now(),
    
    -- properties
    RelativePath        VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
    Status              int                NOT NULL       DEFAULT(0),
    ProcessingOrder     int                NOT NULL       DEFAULT(0),
    Fingerprint         VARCHAR(255)       COLLATE utf8_unicode_ci      NOT NULL,
    Enabled             boolean            not null       DEFAULT(true),
    IsDirectory         boolean            not null       DEFAULT(false),
    Priority            int                not null       DEFAULT(0),
    
    -- size
    OriginalSize        bigint             NOT NULL       DEFAULT(0),
    FinalSize           bigint             NOT NULL       DEFAULT(0),
    
    -- dates 
    CreationTime        datetime           default           now(),
    LastWriteTime       datetime           default           now(),
    ProcessingStarted   datetime           default           now()      NOT NULL,
    ProcessingEnded     datetime           default           now()      NOT NULL,
    
    -- references
    LibraryUid          varchar(36)        COLLATE utf8_unicode_ci      NOT NULL           DEFAULT(''),
    LibraryName         VARCHAR(100)       COLLATE utf8_unicode_ci      NOT NULL           DEFAULT(''),
    FlowUid             varchar(36)        COLLATE utf8_unicode_ci      NOT NULL           DEFAULT(''),
    FlowName            VARCHAR(100)       COLLATE utf8_unicode_ci      NOT NULL           DEFAULT(''),
    DuplicateUid        varchar(36)        COLLATE utf8_unicode_ci      NOT NULL           DEFAULT(''),
    DuplicateName       VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL           DEFAULT(''),
    NodeUid             varchar(36)        COLLATE utf8_unicode_ci      NOT NULL           DEFAULT(''),
    NodeName            VARCHAR(100)       COLLATE utf8_unicode_ci      NOT NULL           DEFAULT(''),
    WorkerUid           varchar(36)        COLLATE utf8_unicode_ci      NOT NULL           DEFAULT(''),

    -- output
    OutputPath          VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL           DEFAULT(''),
    NoLongerExistsAfterProcessing          boolean                      not null           DEFAULT(false),

    -- json data
    OriginalMetadata    TEXT               COLLATE utf8_unicode_ci      NOT NULL           DEFAULT(''),
    FinalMetadata       TEXT               COLLATE utf8_unicode_ci      NOT NULL           DEFAULT(''),
    ExecutedNodes       TEXT               COLLATE utf8_unicode_ci      NOT NULL           DEFAULT('')
);");
        manager.Execute(sql, null).Wait();
        
        // create indexes if dont exist
        //CREATE INDEX idx_LibraryFile_Status ON LibraryFile(Status);
        //CREATE INDEX idx_LibraryFile_DateModified ON LibraryFile(DateModified);
    }

}