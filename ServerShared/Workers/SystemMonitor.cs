using System.Diagnostics;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Workers;

/// <summary>
/// Worker that monitors system information
/// </summary>
public class SystemMonitor:Worker
{
    public readonly Queue<SystemValue<float>> CpuUsage = new (10_000);
    public readonly Queue<SystemValue<float>> MemoryUsage = new (10_000);
    public readonly Queue<SystemValue<long>> TempStorageUsage = new(10_000);

    /// <summary>
    /// Gets the instance of the system monitor
    /// </summary>
    public static SystemMonitor Instance { get; private set; }
    
    public SystemMonitor() : base(ScheduleType.Second, 2)
    {
        Instance = this;
    }

    protected override void Execute()
    {
        var taskCpu = GetCpu();
        var taskTempStorage = GetTempStorageSize();

        MemoryUsage.Enqueue(new()
        {
            Value = GC.GetTotalMemory(true)
        });
        
        Task.WaitAll(taskCpu, taskTempStorage);
        CpuUsage.Enqueue(new ()
        {
            Value = taskCpu.Result
        });
        
        TempStorageUsage.Enqueue(new ()
        {
            Value = taskTempStorage.Result
        });
    }

    private async Task<float> GetCpu()
    {
        await Task.Delay(1);
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

    private async Task<long> GetTempStorageSize()
    {
        var node = await new NodeService().GetServerNode();
        var tempPath = node?.TempPath;
        if (string.IsNullOrEmpty(tempPath))
            return 0;
        
        try
        {
            var dir = new DirectoryInfo(tempPath);
            if (dir.Exists == false)
                return 0;
            return dir.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(x => x.Length);
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
