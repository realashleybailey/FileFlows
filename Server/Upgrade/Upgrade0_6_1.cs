using FileFlows.Server.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

public class Upgrade0_6_1
{
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.6.1 upgrade script");
        
        settings.CompressLibraryFileLogs = true;
        
        new LogConverter().Run();
    }
}
