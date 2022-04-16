using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

public class UpgradeStats
{
    public void Run()
    {
        Logger.Instance.ILog("Upgrade running, adding statistics, this may take a while depending on number of processed library files");
        var libraryFiles = new LibraryFileController().GetAll(FileStatus.Processed).Result;
        var stats = new StatisticsController().Get().Result;
        int count = 0;
        foreach(var lf in libraryFiles)
        {
            if (lf?.ExecutedNodes?.Any() != true)
                continue;
            foreach(var node in lf.ExecutedNodes)
            {
                stats.RecordNode(node);
                ++count;
            }
        }
        DbHelper.Update(stats).Wait();
        Logger.Instance.ILog($"Recording {count} executed node in statistics");
    }
}
