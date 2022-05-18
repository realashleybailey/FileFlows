using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

public class Upgrade0_6_1
{
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.6.1 upgrade script");
        
        new LogConverter().Run();

        UpdateLibraryFileDates();
    }

    private void UpdateLibraryFileDates()
    {
        var files = DbHelper.Select<LibraryFile>().Result;
        foreach (var lf in files)
        {
            if (lf.CreationTime < new DateTime(1900, 1, 1))
            {
                // set these values to now so they wont trigger a reprocess
                lf.CreationTime = DateTime.Now;
                lf.LastWriteTime = DateTime.Now;
                DbHelper.Update(lf).Wait();
            }
        }
        var controller = new LibraryFileController();
        controller.ClearData();
    }
}
