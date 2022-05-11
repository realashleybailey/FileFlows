using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

public class Upgrader
{
    public void Run(Settings settings)
    {
        var currentVersion = string.IsNullOrWhiteSpace(settings.Version) ? new Version() : Version.Parse(settings.Version);

        if(currentVersion <  new Version(0, 5, 0))
        {
            // no stats recorded, we need to add this
            new UpgradeStats().Run();
        }
        if (currentVersion < new Version(0, 5, 3))
        {
            // no stats recorded, we need to add this
            new Upgrade0_5_3().Run();
        }
        if (currentVersion < new Version(0, 6, 0))
        {
            // directory changes, ffmpeg on windows directory changed
            new Upgrade0_6_0().Run();
        }
        
        

        // save the settings
        if (settings.Version != Globals.Version)
        {
            settings.Version = Globals.Version;
            DbManager.Update(settings).Wait();
        }
    }
}
