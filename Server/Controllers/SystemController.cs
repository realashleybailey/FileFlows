using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Diagnostics;
using System.Reactive.Linq;
using FileFlows.Node.Workers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Workers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using FileHelper = FileFlows.ServerShared.Helpers.FileHelper;

namespace FileFlows.Server.Controllers;

/// <summary>
/// System Controller
/// </summary>
[Route("/api/system")]
public class SystemController:Controller
{
    /// <summary>
    /// Gets the version of FileFlows
    /// </summary>
    [HttpGet("version")]
    public string GetVersion() => Globals.Version;

    /// <summary>
    /// Gets the version an node update available
    /// </summary>
    /// <returns>the version an node update available</returns>
    [HttpGet("node-update-version")]
    public string GetNodeUpdateVersion()
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return string.Empty;
        return Globals.Version;
    }
    
    /// <summary>
    /// Gets an node update available
    /// </summary>
    /// <param name="version">the current version of the node</param>
    /// <param name="windows">if the update is for a windows system</param>
    /// <returns>if there is a node update available, returns the update</returns>
    [HttpGet("node-updater-available")]
    public IActionResult GetNodeUpdater([FromQuery]string version, [FromQuery] bool windows)
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return new ContentResult();
        if (string.IsNullOrWhiteSpace(version))
            return new ContentResult();
        var current = new Version(Globals.Version);
        var node =  new Version(version);
        if (node >= current)
            return new ContentResult();

        return GetNodeUpdater(windows);
    }

    /// <summary>
    /// Gets the node updater
    /// </summary>
    /// <param name="windows">if the update is for a windows system</param>
    /// <returns>the node updater</returns>
    [HttpGet("node-updater")]
    public IActionResult GetNodeUpdater([FromQuery] bool windows)
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return new ContentResult();
        
        string updateFile = Path.Combine(DirectoryHelper.BaseDirectory, "Server", "Nodes",
            $"FileFlows-Node-{Globals.Version}.zip");
        if (System.IO.File.Exists(updateFile) == false)
            return new ContentResult();

        return File(System.IO.File.ReadAllBytes(updateFile), "application/zip");
    }

    /// <summary>
    /// Pauses the system
    /// </summary>
    /// <param name="duration">duration in minutes to pause for, any number less than 1 will resume</param>
    [HttpPost("pause")]
    public async Task Pause([FromQuery] int duration)
    {
        var controller = new SettingsController();
        var settings = await controller.Get();
        if (duration < 1)
            settings.PausedUntil = DateTime.MinValue;
        else
            settings.PausedUntil = DateTime.Now.AddMinutes(duration);
        
        await controller.Save(settings);
    }


    /// <summary>
    /// Gets the system information for the FileFlows server,
    /// which includes memory and CPU usage
    /// </summary>
    /// <returns></returns>
    [HttpGet("info")]
    public async Task<SystemInfo> GetSystemInfo()
    {
        SystemInfo info = new ();
        //Process proc = Process.GetCurrentProcess();
        //info.MemoryUsage = proc.PrivateMemorySize64;
        info.MemoryUsage = GC.GetTotalMemory(true);
        info.CpuUsage = await GetCpuPercentage();
        var settings = await new SettingsController().Get();
        info.IsPaused = settings.IsPaused;
        info.PausedUntil = settings.PausedUntil;
        return info;
    }

    /// <summary>
    /// Gets history CPU data of system information
    /// </summary>
    /// <param name="since">data since a date</param>
    /// <returns>the history CPU data</returns>
    [HttpGet("history-data/cpu")]
    public IEnumerable<SystemValue<float>> GetCpuData([FromQuery] DateTime? since = null)
    {
        if (since == null)
            return SystemMonitor.Instance.CpuUsage;
        return SystemMonitor.Instance.CpuUsage.Where(x => x.Time > since);
    }
    
    /// <summary>
    /// Gets history memory data of system information
    /// </summary>
    /// <param name="since">data since a date</param>
    /// <returns>the history memory data</returns>
    [HttpGet("history-data/memory")]
    public IEnumerable<SystemValue<float>> GetMemoryData([FromQuery] DateTime? since = null)
    {
        if (since == null)
            return SystemMonitor.Instance.MemoryUsage;
        return SystemMonitor.Instance.MemoryUsage.Where(x => x.Time > since);
    }
    
    /// <summary>
    /// Gets history temporary storage data of system information
    /// </summary>
    /// <param name="since">data since a date</param>
    /// <returns>the history temporary storage data</returns>
    [HttpGet("history-data/temp-storage")]
    public IEnumerable<SystemValue<long>> GetTempStorageData([FromQuery] DateTime? since = null)
    {
        if (since == null)
            return SystemMonitor.Instance.TempStorageUsage;
        return SystemMonitor.Instance.TempStorageUsage.Where(x => x.Time > since);
    }

    /// <summary>
    /// Gets history library processing time data
    /// </summary>
    /// <returns>history library processing time data</returns>
    [HttpGet("history-data/library-processing-time")]
    public async Task<object> GetLibraryProcessingTime()
    {
        if (DbHelper.UseMemoryCache)
            return new object[] { }; // not supported
        var data = await DbHelper.GetLibraryProcessingTimes();
        var dict = data.Select(x => new
        {
            x.Library,
            Value = (x.OriginalSize / 1_000_000d) / x.Seconds
        }).OrderBy(x => x.Value).GroupBy(x => x.Library, x=> x);
        
        return dict.Where(x => x.Count() > 10).Select(x =>
        {
            var list = x.ToList();
            int length = list.Count;
            var median = list[length / 2];
            var lq = list[length / 4];
            var hq = list[length / 4 * 3];
            return new
            {
                x = x.Key?.EmptyAsNull() ?? "Unknown", 
                y = new [] { (int)list[0].Value, (int)lq.Value, (int)median.Value,(int) hq.Value,(int) list[length -1].Value }
            };
        });
    }

    /// <summary>
    /// Gets heat map data for the processing times of the system
    /// </summary>
    /// <returns></returns>
    [HttpGet("history-data/processing-heatmap")]
    public async Task<object> GetProcessingHeatMap()
    {
        if (DbHelper.UseMemoryCache)
            return "Not supported by this installation";
        var data = await DbHelper.GetHourProcessingTotals();
        return data.Select((x, index) => new
        {
            name = ((DayOfWeek)index).ToString()[..3],
            data = x.Select(y => new
            {
                x = y.Key == 0 ? "12am" : y.Key == 12 ? "12pm" : y.Key > 12 ? (y.Key - 12) + "pm" : y.Key + "am",
                y = y.Value
            })
        });
    }

    /// <summary>
    /// Gets history data of system information
    /// </summary>
    /// <param name="since">data since a date</param>
    /// <returns>the history data</returns>
    [HttpGet("history-data")]
    public SystemInfoData GetSystemInfoData([FromQuery] DateTime? since = null)
    {
        if (since == null)
        {
            return new ()
            {
                SystemDateTime = DateTime.Now,
                CpuUsage = GroupData(SystemMonitor.Instance.CpuUsage),
                MemoryUsage = GroupData(SystemMonitor.Instance.MemoryUsage),
                TempStorageUsage = GroupData(SystemMonitor.Instance.TempStorageUsage),
                LibraryProcessingTimes = SystemMonitor.Instance.LibraryProcessingTimes
            };
        }
        
        return new ()
        {
            SystemDateTime = DateTime.Now,
            CpuUsage = SystemMonitor.Instance.CpuUsage.Where(x => x.Time > since),
            MemoryUsage = SystemMonitor.Instance.MemoryUsage.Where(x => x.Time > since),
            TempStorageUsage = SystemMonitor.Instance.TempStorageUsage.Where(x => x.Time > since),
        };

        IEnumerable<SystemValue<T>> GroupData<T>(IEnumerable<SystemValue<T>> data) 
        {
            return data.GroupBy(x =>
                    new DateTime(x.Time.Year, x.Time.Month, x.Time.Day, x.Time.Hour, x.Time.Minute >= 30 ? 30 : 0, 0))
                .Select(x => new SystemValue<T>
                {
                    Time = x.Key,
                    Value = (T) Convert.ChangeType(x.Average(y => Convert.ToDecimal(y.Value)), typeof(T))
                });
        } 
    }

    private async Task<float> GetCpuPercentage()
    {
        var startTime = DateTime.UtcNow;
        var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        await Task.Delay(100);

        stopWatch.Stop();
        var endTime = DateTime.UtcNow;
        var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

        var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        var totalMsPassed = (endTime - startTime).TotalMilliseconds;
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

        var cpuUsagePercentage = (float)(cpuUsageTotal * 100);
        return cpuUsagePercentage;
    }

    /// <summary>
    /// Restarts FileFlows server
    /// </summary>
    [HttpPost("restart")]
    public void Restart()
    {
        if (Program.Docker == false)
        {
            string script = Path.Combine(DirectoryHelper.BaseDirectory, "Server",
                "restart." + (Globals.IsWindows ? "bat" : "sh"));
            if (Globals.IsLinux)
                FileHelper.MakeExecutable(script);
            
            var psi = new ProcessStartInfo(script);
            psi.ArgumentList.Add(Process.GetCurrentProcess().Id.ToString());
            psi.WorkingDirectory = Path.Combine(DirectoryHelper.BaseDirectory, "Server");
            psi.UseShellExecute = true;
            psi.CreateNoWindow = true;
#if(!DEBUG)
            Process.Start(psi);
#endif
        }

        // docker is easy, just stop it and it should auto restart
        WorkerManager.StopWorkers();
        Environment.Exit(99);
    }

    /// <summary>
    /// Records the node system statistics to the server
    /// </summary>
    /// <param name="args">the node system statistics</param>
    [HttpPost("node-system-statistics")]
    public void RecordNodeSystemStatistics(NodeSystemStatistics args)
    {
        SystemMonitor.Instance.Record(args);
    }
}