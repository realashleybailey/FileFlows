using System.Text.RegularExpressions;
using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Workers;
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
        UpdateScriptReferences();
        
        // update object references
        new ObjectReferenceUpdater().Run();
    }

    private void UpdateScriptReferences()
    {
        var manager = DbHelper.GetDbManager();
        var flows = manager.Select<Flow>().Result.ToList();
        Regex rgxScript = new Regex("(?<=(^Script:[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12}:))(.*?)$");
        foreach (var flow in flows)
        {
            bool changed = false;
            foreach (var part in flow.Parts)
            {
                if (string.IsNullOrWhiteSpace(part?.FlowElementUid))
                    continue;
                var match = rgxScript.Match(part.FlowElementUid);
                if (match.Success == false)
                    continue;

                string oldFlowElementUid = part.FlowElementUid;
                string oldName = match.Value.Trim();
                string newName = GetNewScriptName(oldName);
                part.FlowElementUid = "Script:" + newName;
                changed = true;
                Logger.Instance.ILog($"Updating flow '{flow.Name}' script reference '{oldFlowElementUid}' to '{part.FlowElementUid}'");
            }

            if (changed)
            {
                manager.Update(flow).Wait();
            }
        }
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
            string safeName = GetNewScriptName(dbo.Name);
            string name = new FileInfo(Path.Combine(DirectoryHelper.ScriptsDirectoryFlowUser, safeName + ".js")).FullName;
            File.WriteAllText(name, code);
            Logger.Instance.ILog($"Exported script '{dbo.Name}' to: {name}");
        }

        manager.Execute("delete from DbObject where Type = 'FileFlows.Shared.Models.Script'", null);
    }

    private string GetNewScriptName(string oldName)
    {
        string name = oldName.Replace(": ", " - ");
        foreach (char c in "<>:\"/\\|?*")
            name = name.Replace(c.ToString(), string.Empty);
        return name;
    }

    class CodeObject
    {
        public string Code { get; set; }
    }
}