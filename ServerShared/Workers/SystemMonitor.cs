using System.Diagnostics;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Workers;

/// <summary>
/// Worker that monitors system information
/// </summary>
public class SystemMonitor:Worker
{
    public readonly Queue<SystemValue<float>> CpuUsage = new (1_000);
    public readonly Queue<SystemValue<float>> MemoryUsage = new (1_000);
    public readonly Queue<SystemValue<long>> TempStorageUsage = new(1_000);
    private readonly Dictionary<Guid, NodeSystemStatistics> NodeTemporaryStorage = new();
    /// <summary>
    /// Gets or sets the processing times for libraries
    /// </summary>
    public IEnumerable<BoxPlotData> LibraryProcessingTimes { get; set; }
    

    /// <summary>
    /// Gets the instance of the system monitor
    /// </summary>
    public static SystemMonitor Instance { get; private set; }
    
    public SystemMonitor() : base(ScheduleType.Second, 2)
    {
        Instance = this;
        LibraryProcessingTimes = new BoxPlotData[]
        {
            new() { Name = "Library 1", Minimum = 10, LowQuartile = 20, Median = 28, HighQuartile = 40, Maximum = 70 },
            new() { Name = "Library 2", Minimum = 15, LowQuartile = 32, Median = 45, HighQuartile = 63, Maximum = 96 },
            new() { Name = "Library 3", Minimum = 5, LowQuartile = 14, Median = 32, HighQuartile = 45, Maximum = 60 },
            new() { Name = "Library 4", Minimum = 22, LowQuartile = 40, Median = 70, HighQuartile = 90, Maximum = 120 },
            new() { Name = "Library 5", Minimum = 17, LowQuartile = 28, Median = 54, HighQuartile = 76, Maximum = 104 },
        };
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
        long size = 0;
        if (string.IsNullOrEmpty(tempPath) == false)
        {
            try
            {
                var dir = new DirectoryInfo(tempPath);
                if (dir.Exists)
                    size = dir.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(x => x.Length);
            }
            catch (Exception)
            {
            }
        }

        lock (NodeTemporaryStorage)
        {
            foreach (var nts in NodeTemporaryStorage.Values)
            {
                if (nts.RecordedAt > DateTime.Now.AddMinutes(-5))
                    size += nts.TemporaryDirectorySize;
            }
        }

        return size;
    }

    /// <summary>
    /// Records the node system statistics to the server
    /// </summary>
    /// <param name="args">the node system statistics</param>
    public void Record(NodeSystemStatistics args)
    {
        args.RecordedAt = DateTime.Now;
        lock (NodeTemporaryStorage)
        {
            if (NodeTemporaryStorage.ContainsKey(args.Uid))
                NodeTemporaryStorage[args.Uid] = args;
            else
                NodeTemporaryStorage.Add(args.Uid, args);
        }
    }
}
