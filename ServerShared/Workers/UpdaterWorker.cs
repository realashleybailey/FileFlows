namespace FileFlows.ServerShared.Workers;

using System.Diagnostics;
using System.IO.Compression;

/// <summary>
/// Worker that will automatically update the system
/// </summary>
public abstract class UpdaterWorker:Worker
{
    /// <summary>
    /// Gets if there is an updated pending installation
    /// </summary>
    public static bool UpdatePending { get; private set; }
    protected Version CurrentVersion { get; init; }

    private readonly string UpgradeScriptPrefix;
    
    /// <summary>
    /// Constructs an instance of a Update Worker
    /// </summary>
    /// <param name="upgradeScriptPrefix">The script to execute in the upgrade zip file</param>
    /// <param name="minutes">how many minute between checks</param>
    public UpdaterWorker(string upgradeScriptPrefix, int minutes) : base(ScheduleType.Minute, minutes)
    {
        CurrentVersion = Version.Parse(Globals.Version);
        this.UpgradeScriptPrefix = upgradeScriptPrefix;
    }

    protected override void Execute()
    {
        RunCheck();
    }

    /// <summary>
    /// Gets if the update can run
    /// </summary>
    protected abstract bool CanUpdate();

    /// <summary>
    /// Quits the current application
    /// </summary>
    protected abstract void QuitApplication();
    
    /// <summary>
    /// Runs a check for update and if found will download it 
    /// </summary>
    /// <returns>A update has been downloaded</returns>
    public bool RunCheck()
    {
        Logger.Instance?.ILog("AutoUpdater: Checking for update");
        try
        {
#if(DEBUG)
            return false; // disable during debugging
#else       
            string updateScript = DownloadUpdate();
            if (string.IsNullOrEmpty(updateScript))
                return false;

            UpdatePending = true;
            do
            {
                // sleep just in case something has just started
                Thread.Sleep(10_000);
            } while (CanUpdate());

            RunUpdateScript(updateScript);
            return true;
#endif
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

            QuitApplication();
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("AutoUpdater: Failed running update script: " + ex.Message);
        }
    }

    /// <summary>
    /// Downloads an update
    /// </summary>
    /// <returns>The update file</returns>
    protected abstract string DownloadUpdateBinary();

    /// <summary>
    /// Gets if auto updates are enabled
    /// </summary>
    /// <returns>if auto updates are enabled</returns>
    protected abstract bool GetAutoUpdatesEnabled();
    
    private string DownloadUpdate()
    {
        try
        {
            if(GetAutoUpdatesEnabled() == false)
                return string.Empty;

            string update = DownloadUpdateBinary();
            if (string.IsNullOrEmpty(update))
            {
                Shared.Logger.Instance.DLog("AutoUpdater: No update available");
                return string.Empty;
            }

            var updateDir = new FileInfo(update).DirectoryName;
            
            Logger.Instance.ILog("AutoUpdater: Extracting update to: " + updateDir);
            ZipFile.ExtractToDirectory(update, updateDir);
            Logger.Instance.ILog("AutoUpdater: Extracted update to: " + updateDir);
            // delete the upgrade file after extraction
            File.Delete(update);
            Logger.Instance.ILog("AutoUpdater: Deleted update file: " + update);

            var updateFile = Path.Combine(updateDir, UpgradeScriptPrefix + (Globals.IsWindows ? ".bat" : ".sh"));
            if (File.Exists(updateFile) == false)
            {
                Logger.Instance.WLog("AutoUpdater: No update script found: " + updateFile);
                return string.Empty;
            }
            Logger.Instance.ILog("AutoUpdater: Update script found: " + updateFile);

            if (Globals.IsLinux && MakeExecutable(updateFile) == false)
            {
                Logger.Instance.WLog("AutoUpdater: Failed to make update script executable");
                return string.Empty;
            }

            Logger.Instance.ILog("AutoUpdater: Upgrade directory ready: " + updateDir);
            Logger.Instance.ILog("AutoUpdater: Upgrade script ready: " + updateFile);

            return updateFile;
        }
        catch (Exception ex) 
        {
            if (ex.Message == "Object reference not set to an instance of an object")
                return string.Empty; // just ignore this error, likely due ot it not being configured yet.
            Logger.Instance?.ELog("AutoUpdater: Failed checking for update: " + ex.Message);
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
            Logger.Instance?.ELog("AutoUpdater: Failed making executable:" + process.StartInfo.FileName,
                process.StartInfo.Arguments + Environment.NewLine + output);
            if (string.IsNullOrWhiteSpace(error) == false)
                Logger.Instance?.ELog("AutoUpdater: Error output:" + output);
            return false;
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("AutoUpdater: Failed making executable: " + file + " => " + ex.Message);
            return false;
        }
    }
}