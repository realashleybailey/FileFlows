using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that will automatically delete logs for non existing library files
/// </summary>
public class LogPruner:Worker
{
    /// <summary>
    /// Constructor for the log pruner
    /// </summary>
    public LogPruner() : base(ScheduleType.Hourly, 3)
    {
    }

    protected override void Execute()
    {
        var libFiles = new LibraryFileController().GetDataList().Result.Select(x => x.Uid.ToString()).ToList();
        var files = Directory.GetFiles(DirectoryHelper.LibraryFilesLoggingDirectory);
        foreach (var file in files)
        {
            bool exists = file.IndexOf(".", StringComparison.Ordinal) > 0 
                          && libFiles.Contains(file[..file.IndexOf(".", StringComparison.Ordinal)]);

            if (exists)
                continue;
            try
            {
                File.Delete(file);
                Shared.Logger.Instance?.DLog("Deleted old unknown log file: " + file);
            }
            catch (Exception)
            {
            }
        }
    }
}