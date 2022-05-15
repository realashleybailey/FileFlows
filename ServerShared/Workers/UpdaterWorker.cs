using FileFlows.Shared.Helpers;

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

    protected readonly string UpdaterName; 

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
        UpdaterName = this.GetType().Name;
        RunCheck();
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
        Logger.Instance?.ILog($"{UpdaterName}: Checking for update");
        try
        {
#if(DEBUG && false)
            return false; // disable during debugging
#else       
            string updateScript = DownloadUpdate();
            if (string.IsNullOrEmpty(updateScript))
                return false;

            UpdatePending = true;
            PrepareApplicationShutdown();
            Logger.Instance.ILog($"{UpdaterName}: Update pending installation");
            do
            {
                Logger.Instance.ILog($"{UpdaterName}: Waiting to run update");
                // sleep just in case something has just started
                Thread.Sleep(10_000);
            } while (CanUpdate() == false);

            Logger.Instance.ILog($"{UpdaterName} - Update about to be installed");
            RunUpdateScript(updateScript);
            return true;
#endif
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog($"{UpdaterName} Error: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Prepares the application to be shutdown
    /// Called after the update has been downloaded, but before it has run
    /// </summary>
    protected virtual void PrepareApplicationShutdown()
    {
    }

    private void RunUpdateScript(string updateScript)
    {
        try
        {
            // if inside docker we just restart, the restart policy should automatically kick in then run the upgrade script when it starts
            if (Globals.IsDocker == false) 
            {
                Logger.Instance?.ILog($"{UpdaterName}About to execute upgrade script: " + updateScript);
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
            Logger.Instance?.ELog($"{UpdaterName}: Failed running update script: " + ex.Message);
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

            Logger.Instance?.DLog($"{UpdaterName}: Checking for new update binary");
            string update = DownloadUpdateBinary();
            if (string.IsNullOrEmpty(update))
            {
                Logger.Instance?.DLog($"{UpdaterName}: No update available");
                return string.Empty;
            }
            Logger.Instance?.DLog($"{UpdaterName}: Downloaded update: " + update);

            var updateDir = new FileInfo(update).DirectoryName;
            
            Logger.Instance?.ILog($"{UpdaterName}: Extracting update to: " + updateDir);
            ZipFile.ExtractToDirectory(update, updateDir, true);
            Logger.Instance?.ILog($"{UpdaterName}: Extracted update to: " + updateDir);
            // delete the upgrade file after extraction
            File.Delete(update);
            Logger.Instance?.ILog($"{UpdaterName}: Deleted update file: " + update);

            var updateFile = Path.Combine(updateDir, UpgradeScriptPrefix + (Globals.IsWindows ? ".bat" : ".sh"));
            if (File.Exists(updateFile) == false)
            {
                Logger.Instance?.WLog($"{UpdaterName}: No update script found: " + updateFile);
                return string.Empty;
            }
            Logger.Instance?.ILog($"{UpdaterName}: Update script found: " + updateFile);

            if (Globals.IsLinux && MakeExecutable(updateFile) == false)
            {
                Logger.Instance?.WLog($"{UpdaterName}: Failed to make update script executable");
                return string.Empty;
            }

            Logger.Instance?.ILog($"{UpdaterName}: Upgrade directory ready: " + updateDir);
            Logger.Instance?.ILog($"{UpdaterName}: Upgrade script ready: " + updateFile);

            return updateFile;
        }
        catch (Exception ex) 
        {
            //if (ex.Message == "Object reference not set to an instance of an object")
            //    return string.Empty; // just ignore this error, likely due ot it not being configured yet.
            Logger.Instance?.ELog($"{UpdaterName}: Failed checking for update: " + ex.Message);
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
            Logger.Instance?.ELog($"{UpdaterName}: Failed making executable:" + process.StartInfo.FileName,
                process.StartInfo.Arguments + Environment.NewLine + output);
            if (string.IsNullOrWhiteSpace(error) == false)
                Logger.Instance?.ELog($"{UpdaterName}: Error output:" + output);
            return false;
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog($"{UpdaterName}: Failed making executable: " + file + " => " + ex.Message);
            return false;
        }
    }

    
    /// <summary>
    /// Downloads a file and saves it
    /// </summary>
    /// <param name="url">The url of the file to download</param>
    /// <param name="file">the location to save the file</param>
    /// <exception cref="Exception">throws if the file fails to download</exception>
    protected async Task DownloadFile(string url, string file)
    {
        var result = await HttpHelper.Get<byte[]>(url);
        if (result.Success == false)
            throw new Exception("Failed to get update: " + result.Body);
        await File.WriteAllBytesAsync(file, result.Data);
    }
}