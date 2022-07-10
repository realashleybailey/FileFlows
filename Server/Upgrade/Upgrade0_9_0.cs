using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v0.9.0
/// </summary>
public class Upgrade0_9_0
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.9.0 upgrade script");
        AddStatisticsTable();
        ExportScripts();
    }

    /// <summary>
    /// Adds indexes to the log message table
    /// </summary>
    private void AddStatisticsTable()
    {
        if(DbHelper.GetDbManager() is SqliteDbManager sqlite)
            sqlite.Execute(sqlite.CreateDbStatisticTableScript, new object[]{});
    }

    private void ExportScripts()
    {
        var manager = DbHelper.GetDbManager();
        var dbObjects = manager.Fetch<DbObject>("select * from DbObject where Type = 'FileFlows.Shared.Models.Script'").Result;
        string[] system = new[]
        {
            "7Zip: Compress to Zip", "File: Older Than", "Video: Downscale greater than 1080p",
            "Video: Bitrate greater than", "Video: Has Audio", "Video: Resolution", "NVIDIA: Below Encoder Limit"
        }; 
        foreach (var dbo in dbObjects)
        {
            if (system.Contains(dbo.Name))
                continue;
            string code = JsonSerializer.Deserialize<CodeObject>(dbo.Data).Code;
            string safeName = dbo.Name.Replace(": ", " - ");
            foreach (char c in "<>:\"/\\|?*")
                safeName = safeName.Replace(c.ToString(), string.Empty);
            string name = new FileInfo(Path.Combine(DirectoryHelper.ScriptsDirectory, safeName)).FullName;
            File.WriteAllText(name, code);
            Logger.Instance.ILog($"Exported script '{dbo.Name}' to: {name}");
        }

        //manager.Execute("delete from DbObject where Type = 'FileFlows.Shared.Models.Script'", null);
    }

    class CodeObject
    {
        public string Code { get; set; }
    }
}