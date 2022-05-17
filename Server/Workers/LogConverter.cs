using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that will convert logs to/from a compressed format, depending on the system setting
/// </summary>
public class LogConverter:Worker
{
    /// <summary>
    /// Creates a new instance of the Log Converter 
    /// </summary>
    public LogConverter() : base(ScheduleType.Hourly, 3)
    {
    }

    protected override void Execute()
    {
        Run();
    }
     
    /// <summary>
    /// Runs the log converter
    /// </summary>
    internal void Run()
    {
        bool compress = new SettingsController().Get().Result.CompressLibraryFileLogs = true;
        var files = new DirectoryInfo(DirectoryHelper.LibraryFilesLoggingDirectory).GetFiles();
        foreach (var file in files)
        {
            if(file.LastWriteTime > DateTime.Now.AddHours(-3))
                continue; //file is too new, dont process it yet
            
            if (file.Extension == ".log" && compress)
            {
                // need to create a gz file
                Gzipper.CompressFile(file.FullName, file.FullName[..^4] + ".log.gz", true);
                continue;
            }
            if (file.FullName.EndsWith(".log.gz") && compress == false)
            {
                // need to create a unzip it
                Gzipper.DecompressFile(file.FullName, file.FullName[..^3], true);
                continue;
            }
            
            if (file.FullName.EndsWith(".html"))
            {
                // old html log file, compress it
                Gzipper.CompressFile(file.FullName, file.FullName + ".gz", true);
                continue;
            }
        }
    }
}