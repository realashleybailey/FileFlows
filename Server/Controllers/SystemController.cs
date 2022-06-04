using System.Diagnostics;
using System.IO.Compression;
using FileFlows.Server.Workers;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

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
    /// Gets an node update available
    /// </summary>
    /// <param name="version">the current version of the node</param>
    /// <param name="windows">if the update is for a windows system</param>
    /// <returns>if there is a node update available, returns the update</returns>
    [HttpGet("node-updater-available")]
    public IActionResult GetNodeUpdater([FromQuery]string version, [FromQuery] bool windows)
    {
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
        string updateFile = Path.Combine(DirectoryHelper.BaseDirectory, "Server", "Nodes",
            $"FileFlows-Node-{Globals.Version}.zip");
        if (System.IO.File.Exists(updateFile) == false)
            return new ContentResult();

        return File(System.IO.File.ReadAllBytes(updateFile), "application/zip");
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
        return info;
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
    /// Restarts FileFLow server
    /// </summary>
    [HttpPost("restart")]
    public void Restart()
    {
        WorkerManager.StopWorkers();
        Environment.Exit(99);
    }
}