using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Services;

namespace FileFlows.ServerShared.Workers;

/// <summary>
/// Worker to clean up old log files
/// </summary>
public class LogFileCleaner:Worker
{
    /// <summary>
    /// Constructs a log file cleaner
    /// </summary>
    public LogFileCleaner() : base(ScheduleType.Hourly, 1)
    {
        Execute();
    }

    /// <summary>
    /// Executes the cleaner
    /// </summary>
    protected sealed override void Execute()
    {
        var settings = new SettingsService().Get().Result;
        if (settings == null || settings.LogFileRetention < 1)
            return; // not yet ready
        var dir = DirectoryHelper.LoggingDirectory;
        int count = 0;
        foreach (var file in new DirectoryInfo(dir).GetFiles("FileFlows*.log").OrderByDescending(x => x.LastWriteTime))
        {
            if (++count > settings.LogFileRetention)
            {
                try
                {
                    file.Delete();
                    Logger.Instance.ILog("Deleted log file: " + file.Name);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
}