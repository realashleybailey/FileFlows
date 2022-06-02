using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

public class Upgrade0_7_0
{
    public void Run(Settings settings)
    {
        RemovePluginsFromDatabase();
    }

    private void RemovePluginsFromDatabase()
    {
	    // we remove these as we now have constant UID for the plugins
	    DbHelper.Delete<PluginInfo>("");
        
        // now scan the plugins again
        PluginScanner.Scan();
    }
}