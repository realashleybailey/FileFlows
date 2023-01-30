using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v1.0.9
/// </summary>
public class Upgrade_1_0_9
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 1.0.9 upgrade script");
        AddLibraryFileFlags();
    }

    private void AddLibraryFileFlags()
    {
        var manager = DbHelper.GetDbManager();

        manager.Execute(@"
ALTER TABLE LibraryFile
ADD Flags               int                not null     DEFAULT(0)
".Trim(), null).Wait();
    }
}