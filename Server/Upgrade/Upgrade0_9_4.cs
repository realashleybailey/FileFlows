using System.Text.RegularExpressions;
using FileFlows.Server.Controllers;
using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v0.9.4
/// </summary>
public class Upgrade0_9_4
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.9.4 upgrade script");
        RenameToolsToVariables();
        MoveScripts();
    }

    private void RenameToolsToVariables()
    {
        var manager = DbHelper.GetDbManager();
        manager.Execute(
            "update DbObject set Type = 'FileFlows.Shared.Models.Variable', Data = replace(Data, 'Path', 'Value') where Type = 'FileFlows.Shared.Models.Tool'", null);
    }

    private void MoveScripts()
    {
        var oldDir = new DirectoryInfo(Path.Combine(DirectoryHelper.ScriptsDirectory, "User"));
        if (oldDir.Exists == false)
            return;
        foreach (var file in oldDir.GetFiles())
        {
            file.MoveTo(Path.Combine(DirectoryHelper.ScriptsDirectoryFlowUser));
        }
        oldDir.Delete();
    }

}