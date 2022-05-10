using System.IO.Compression;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Node.Workers;

public class NodeUpdater:Worker
{
    internal static bool UpdatePending { get; private set; }
    private Version CurrentVersion { get; init; }
    public NodeUpdater() : base(ScheduleType.Minute, 1)
    {
        CurrentVersion = Version.Parse(Globals.Version);
    }

    protected override void Execute()
    {
        RunCheck();
    }
    
    internal bool RunCheck()
    {
        Logger.Instance?.ILog("Checking for Node update");
        try
        {
#if(DEBUG)
            return false; // disable during debugging
#endif
            string updateScript = DownloadUpdate();
            if (string.IsNullOrEmpty(updateScript))
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
        catch (Exception)
        {
            return false;
        }
    }

    private void RunUpdateScript(string updateScript)
    {
        try
        {
            // if inside docker we just restart, the restart policy should automatically kick in then run the upgrade script when it starts
            if (Globals.IsDocker == false) 
            {
                Logger.Instance?.ILog("About to execute upgrade script: " + updateScript);
                var fi = new FileInfo(updateScript);

                var psi = new ProcessStartInfo(updateScript);
                psi.ArgumentList.Add(Process.GetCurrentProcess().Id.ToString());
                psi.WorkingDirectory = fi.DirectoryName;
                psi.UseShellExecute = true;
                psi.CreateNoWindow = true;
                Process.Start(psi);
            }

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
            if (settings?.AutoUpdateNodes != true)
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

            var updateDir = Path.Combine(DirectoryHelper.BaseDirectory, "NodeUpdate");
            if (Directory.Exists(updateDir)) // delete the update dir so we get a full fresh update
                Directory.Delete(updateDir, true);
            Directory.CreateDirectory(updateDir);

            string update = Path.Combine(updateDir, "update.zip");
            File.WriteAllBytes(update, data);
            ZipFile.ExtractToDirectory(update, updateDir);
            // delete the upgrade file after extraction
            File.Delete(update);

            var updateFile = Path.Combine(updateDir, "node-upgrade" + (Globals.IsWindows ? ".bat" : ".sh"));
            if (File.Exists(updateFile) == false)
            {
                Logger.Instance.WLog("No update script found: " + updateFile);
                return string.Empty;
            }

            if (Globals.IsLinux && MakeExecutable(updateFile) == false)
                return string.Empty;

            return updateFile;
        }
        catch (Exception ex) 
        {
            if (ex.Message == "Object reference not set to an instance of an object")
                return string.Empty; // just ignore this error, likely due ot it not being configured yet.
            Logger.Instance?.ELog("Failed checking for node update: " + ex.Message);
            return string.Empty;
        }
    }

    private bool MakeExecutable(string file)
    {
        try
        {
            var fi = new FileInfo(file);
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo("/bin/bash", $"-c \"chmod +x {fi.Name}\"")
            {
                WorkingDirectory = fi.DirectoryName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

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