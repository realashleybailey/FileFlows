using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v1.0.0
/// </summary>
public class Upgrade_1_0_0
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 1.0.0 upgrade script");
        RenameToolsToVariables();
        MoveUserScripts();
        RemoveOldSystemScripts();
    }

    private void RenameToolsToVariables()
    {
        Logger.Instance.ILog("Renaming Tools to Variables");
        var manager = DbHelper.GetDbManager();
        manager.Execute(
            "update DbObject set Type = 'FileFlows.Shared.Models.Variable', Data = replace(Data, 'Path', 'Value') where Type = 'FileFlows.Shared.Models.Tool'", null).Wait();
        manager.Execute(
            "update DbObject set Name = 'ffmpeg' where Type = 'FileFlows.Shared.Models.Variable' and Name = 'FFMpeg'", null).Wait();
    }

    private void MoveUserScripts()
    {
        Logger.Instance.ILog("Moving User Scripts");
        var oldDir = new DirectoryInfo(Path.Combine(DirectoryHelper.ScriptsDirectory, "User"));
        if (oldDir.Exists == false)
            return;
        foreach (var file in oldDir.GetFiles())
        {
            file.MoveTo(Path.Combine(DirectoryHelper.ScriptsDirectoryFlow));
        }
        oldDir.Delete();
    }

    private void RemoveOldSystemScripts()
    {
        Logger.Instance.ILog("Removing old system scripts");
        var oldDir = new DirectoryInfo(Path.Combine(DirectoryHelper.ScriptsDirectory, "System"));
        if (oldDir.Exists == false)
            return;
        foreach(var file in oldDir.GetFiles("*.js"))
            file.Delete();
    }
}