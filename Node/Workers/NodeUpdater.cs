using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Node.Workers;

public class NodeUpdater:UpdaterWorker
{
    public NodeUpdater() : base("node-upgrade", 1)
    {
    }

    protected override bool CanUpdate() => FlowWorker.HasActiveRunners == false;

    protected override void QuitApplication()
    {
        Program.Quit();
    }

    protected override string DownloadUpdateBinary()
    {   
        var systemService = SystemService.Load();
        var serverVersion = systemService.GetVersion().Result;
        if (serverVersion <= CurrentVersion)
            return string.Empty;

        Logger.Instance.ILog($"New Node version {serverVersion} detected, starting download");

        var data = systemService.GetNodeUpdater().Result;
        if (data?.Any() != true)
        {
            Logger.Instance.WLog("Failed to download Node updater.");
            return string.Empty;
        }

        var updateDir = Path.Combine(DirectoryHelper.BaseDirectory, "NodeUpdate");
        if (Directory.Exists(updateDir)) // delete the update dir so we get a full fresh update
            Directory.Delete(updateDir, true);
        Directory.CreateDirectory(updateDir);

        string update = Path.Combine(updateDir, "update.zip");
        File.WriteAllBytes(update, data);
        return update;
    }

    protected override bool GetAutoUpdatesEnabled()
    {
        var settingsService = SettingsService.Load();
        var settings = settingsService.Get().Result;
        return settings?.AutoUpdateNodes == true;
    }
}