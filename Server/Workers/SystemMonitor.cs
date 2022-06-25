using System.Collections.Concurrent;
using System.Diagnostics;
using FileFlows.Server.Database.Managers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that monitors system information
/// </summary>
public class SystemMonitor:Worker
{
    public readonly FixedSizedQueue<SystemValue<float>> CpuUsage = new (1_000);
    public readonly FixedSizedQueue<SystemValue<float>> MemoryUsage = new (1_000);
    public readonly FixedSizedQueue<SystemValue<long>> TempStorageUsage = new(1_000);
    public readonly FixedSizedQueue<SystemValue<long>> LogStorageUsage = new(1_000);
    public readonly FixedSizedQueue<SystemValue<long>> OpenDbConnections = new(1_000);
    private readonly Dictionary<Guid, NodeSystemStatistics> NodeStatistics = new();
    

    /// <summary>
    /// Gets the instance of the system monitor
    /// </summary>
    public static SystemMonitor Instance { get; private set; }
    
    public SystemMonitor() : base(ScheduleType.Second, 10)
    {
        Instance = this;
    }

    protected override void Execute()
    {
        var taskCpu = GetCpu();
        var taskTempStorage = GetTempStorageSize();
        var taskLogStorage = GetLogStorageSize();

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
        LogStorageUsage.Enqueue(new ()
        {
            Value = taskLogStorage.Result
        });
        OpenDbConnections.Enqueue((new ()
        {
            Value = DbManager.GetOpenDbConnections()
        }));
    }

    private async Task<float> GetCpu()
    {
        await Task.Delay(1);
        List<float> records = new List<float>();
        int max = 7;
        for (int i = 0; i <= max; i++)
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

            records.Add((float)(cpuUsageTotal * 100));
            if (i == max)
                break;
            await Task.Delay(1000);
        }

        return records.Max();
    }

    private async Task<long> GetTempStorageSize()
    {
        var node = await new NodeService().GetServerNode();
        var tempPath = node?.TempPath;
        return GetDirectorySize(tempPath);
    }

    private async Task<long> GetLogStorageSize()
    {
        await Task.Delay(1);
        string logPath = DirectoryHelper.LoggingDirectory;
        string libFileLogPath = DirectoryHelper.LibraryFilesLoggingDirectory;
        if(libFileLogPath == null || logPath.Contains(libFileLogPath))
            return GetDirectorySize(logPath, logginDir: true);
        long logPathLength = GetDirectorySize(logPath);
        long libFileLogPathLength = GetDirectorySize(libFileLogPath);
        return logPathLength + libFileLogPathLength;
    }
    
    
    private long GetDirectorySize(string path, bool logginDir = false)
    {
        long size = 0;
        if (string.IsNullOrEmpty(path) == false)
        {
            try
            {
                var dir = new DirectoryInfo(path);
                if (dir.Exists)
                    size = dir.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(x => x.Length);
            }
            catch (Exception)
            {
            }
        }

        lock (NodeStatistics)
        {
            foreach (var nts in NodeStatistics.Values)
            {
                if (nts.RecordedAt > DateTime.Now.AddMinutes(-5))
                {
                    size += logginDir ? nts.LogDirectorySize : nts.TemporaryDirectorySize;
                }
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
        lock (NodeStatistics)
        {
            if (NodeStatistics.ContainsKey(args.Uid))
                NodeStatistics[args.Uid] = args;
            else
                NodeStatistics.Add(args.Uid, args);
        }
    }
}


/// <summary>
/// A queue of fixed size
/// </summary>
/// <typeparam name="T">the type to queue</typeparam>
public class FixedSizedQueue<T> : ConcurrentQueue<T>
{
    private readonly object syncObject = new object();

    /// <summary>
    /// Gets or sets the max queue size
    /// </summary>
    public int Size { get; private set; }

    /// <summary>
    /// Constructs an instance of a fixed size queue
    /// </summary>
    /// <param name="size">the size of the queue</param>
    public FixedSizedQueue(int size)
    {
        Size = size;
    }

    /// <summary>
    /// Adds a item to the queue
    /// </summary>
    /// <param name="obj">the item to add</param>
    public new void Enqueue(T obj)
    {
        base.Enqueue(obj);
        lock (syncObject)
        {
            while (base.Count > Size)
            {
                T outObj;
                base.TryDequeue(out outObj);
            }
        }
    }
}