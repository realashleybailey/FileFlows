using System.Text.RegularExpressions;
using FileFlows.Server.Controllers;
using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Workers;
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
        MoveSystemScripts();
    }

    private void RenameToolsToVariables()
    {
        var manager = DbHelper.GetDbManager();
        manager.Execute(
            "update DbObject set Type = 'FileFlows.Shared.Models.Variable', Data = replace(Data, 'Path', 'Value') where Type = 'FileFlows.Shared.Models.Tool'", null).Wait();
        manager.Execute(
            "update DbObject set Name = 'ffmpeg' where Type = 'FileFlows.Shared.Models.Variable' and Name = 'FFMpeg'", null).Wait();
    }

    private void MoveUserScripts()
    {
        var oldDir = new DirectoryInfo(Path.Combine(DirectoryHelper.ScriptsDirectory, "User"));
        if (oldDir.Exists == false)
            return;
        foreach (var file in oldDir.GetFiles())
        {
            file.MoveTo(Path.Combine(DirectoryHelper.ScriptsDirectoryFlow));
        }
        oldDir.Delete();
    }

    private void MoveSystemScripts()
    {
        var oldDir = new DirectoryInfo(Path.Combine(DirectoryHelper.ScriptsDirectory, "System"));
        if (oldDir.Exists == false)
            return;
        // system scripts are now repository scripts, and they contain a path on the first line so they can be updated
        foreach (var file in oldDir.GetFiles())
        {
            string path = "Flow/";
            if (file.Name.StartsWith("File -"))
                path += "File/";
            else if (file.Name.StartsWith("NVIDIA -"))
                path += "Hardware/";
            else if (file.Name.StartsWith("7Zip -"))
                path += "Utilities/";
            else if (file.Name.StartsWith("Video -"))
                path += "Video/";
            else
            {
                Logger.Instance.WLog("Unknown system script, cannot upgrade this script: " + file.FullName);
                continue;
            }
            string content = "// path: " + path + "\n\n" + File.ReadAllText(file.FullName);
            File.WriteAllText(Path.Combine(DirectoryHelper.ScriptsDirectoryFlow, file.Name), content);
        }
        oldDir.Delete();
    }
}