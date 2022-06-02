using FileFlows.Server.Controllers;
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
        // get the plugins we need 
        var pluginDir = PluginScanner.GetPluginDirectory();
        var plugins = new DirectoryInfo(pluginDir).GetFiles("*.ffplugin");
        
        
	    // we remove these as we now have constant UID for the plugins
	    DbHelper.Delete<PluginInfo>("");
        
        // download the plugins we just deleted, this ensures they have the UID set
        foreach (var plugin in plugins)
        {
            string name = plugin.Name;
            Logger.Instance.ILog("Updating plugin: " + name);
            // first delete it
            plugin.Delete();
            // now download it
            new PluginController().DownloadPluginFromRepository(name);
        }
        
        // now scan the plugins again
        PluginScanner.Scan();
    }
}