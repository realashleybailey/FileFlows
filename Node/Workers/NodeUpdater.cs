using System.IO.Compression;
using System.Net.Mime;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;
using Microsoft.Extensions.Logging.Abstractions;

namespace FileFlows.Node.Workers;

public class NodeUpdater:Worker
{
    internal static bool UpdatePending { get; private set; }
    private Version CurrentVersion { get; init; }
    public NodeUpdater() : base(ScheduleType.Minute, 5)
    {
        CurrentVersion = Version.Parse(Globals.Version);
    }

    protected override void Execute()
    {
        RunCheck();
    }
    
    internal bool RunCheck()
    {
        #if(DEBUG)
        return false; // disable during debugging
        #endif
        string updateScript = DownloadUpdate();
        if(string.IsNullOrEmpty(updateScript))
            return false;

        UpdatePending = true;
        do
        {
            // sleep just in case something has just started
            Thread.Sleep(10_000);
        } while (FlowWorker.HasActiveRunners);


        RunUpdateScript(updateScript);
        return true;
    }

    private void RunUpdateScript(string updateScript)
    {
        try
        {
            Logger.Instance?.ILog("About to execute upgrade script: " + updateScript);
            var fi = new FileInfo(updateScript);
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(updateScript);
            process.StartInfo.ArgumentList.Add(Process.GetCurrentProcess().Id.ToString());
            process.StartInfo.WorkingDirectory = fi.DirectoryName;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            Program.Quit();
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("Failed running update script: " + ex.Message);
        }
    }

    private string DownloadUpdate()
    {
        try
        {
            var settingsService = SettingsService.Load();
            var settings = settingsService.Get().Result;
            if (settings.AutoUpdateNodes == false)
                return string.Empty;

            var systemService = SystemService.Load();
            var serverVersion = systemService.GetVersion().Result;
            if (serverVersion <= CurrentVersion)
                return string.Empty;

            Logger.Instance.ILog($"New Node version {serverVersion} detected, starting downloaded");

            var data = systemService.GetNodeUpdater().Result;
            if (data?.Any() != true)
            {
                Logger.Instance.WLog("Failed to download Node updater.");
                return string.Empty;
            }

            var updateDir = Path.Combine(Directory.GetCurrentDirectory(), "NodeUpdate");
            if (Directory.Exists(updateDir)) // delete the update dir so we get a full fresh update
                Directory.Delete(updateDir, true);
            Directory.CreateDirectory(updateDir);

            string update = Path.Combine(updateDir, "update.zip");
            File.WriteAllBytes(update, data);
            ZipFile.ExtractToDirectory(update, updateDir);

            var updateFile = Path.Combine(updateDir, "node-upgrade" + (Globals.IsWindows ? ".bat" : ".sh"));
            if (File.Exists(updateFile) == false)
            {
                Logger.Instance.WLog("No update script found: " + updateFile);
                return string.Empty;
            }

            if (Globals.IsLinux)
            {
                if (MakeExecutable(updateFile) == false)
                    return string.Empty;
            }

            return updateFile;
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("Failed downloading node update: " + ex.Message + Environment.NewLine +
                                  ex.StackTrace);
            return string.Empty;
        }
    }

    private bool MakeExecutable(string file)
    {
        try
        {
            var fi = new FileInfo(file);
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo("/bin/bash", $"-c \"chmod +x {fi.Name}\"");
            process.StartInfo.WorkingDirectory = fi.DirectoryName;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardError.ReadToEnd();
            Console.WriteLine(output);
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
                return true;
            Logger.Instance?.ELog("Failed making executable:" + process.StartInfo.FileName,
                process.StartInfo.Arguments + Environment.NewLine + output);
            if (string.IsNullOrWhiteSpace(error) == false)
                Logger.Instance?.ELog("Error output:" + output);
            return false;
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("Failed making executable: " + file + " => " + ex.Message);
            return false;
        }
    }
}