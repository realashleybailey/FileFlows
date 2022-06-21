using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Node.Workers;

/// <summary>
/// Worker that gathers system statistics 
/// </summary>
public class SystemStatisticsWorker:Worker
{
    private ProcessingNode? processingNode; 
    
    /// <summary>
    /// Constructs an instance of hte System Statistcs worker
    /// </summary>
    public SystemStatisticsWorker() : base(ScheduleType.Second, 10)
    {
    }

    protected override void Execute()
    {
        if(processingNode == null)
        {
            var nodeService = NodeService.Load();
            processingNode =
                nodeService.GetByAddress(AppSettings.ForcedHostName?.EmptyAsNull() ?? Environment.MachineName).Result;
            if (processingNode == null)
                return;
        }
        
        var storage = GetTempStorageSize().Result;
        new SystemService().RecordNodeSystemStatistics(new()
        {
            Uid = processingNode.Uid,
            TemporaryDirectorySize = storage
        }).Wait();
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


        return size;
    }
}