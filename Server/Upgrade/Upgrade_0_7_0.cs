using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v0.7.0
/// </summary>
public class Upgrade_0_7_0
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.7.0 upgrade script");
        settings.LogQueueMessages = false;
        #if(!DEBUG)
        RemovePluginsFromDatabase(settings);
        #endif
        UpdateInternalProcessingNodeUid();
    }

    private void UpdateInternalProcessingNodeUid()
    {
        // we changed the internal processing node to use a constant UID
        var node = DbHelper.GetByName<ProcessingNode>("FileFlowsServer").Result;
        if (node == null)
            return;
        Logger.Instance.ILog("Updating UID of Internal Processing Node");
        DbHelper.Delete<ProcessingNode>("Name = @1", node.Name).Wait();
        node.Uid = Globals.InternalNodeUid;
        DbHelper.Update(node);
    }

    private void RemovePluginsFromDatabase(Settings settings)
    {
        Logger.Instance.ILog("Upgrading plugins to version 0.7.0");
        // get the plugins we need 
        var pluginDir = PluginScanner.GetPluginDirectory();
        var plugins = new DirectoryInfo(pluginDir).GetFiles("*.ffplugin");
        Logger.Instance.ILog("Plugins found: " + plugins.Length);
        
	    // we remove these as we now have constant UID for the plugins
	    DbHelper.Delete<PluginInfo>("");

        
        var repos = settings.PluginRepositoryUrls?.Select(x => x)?.ToList() ?? new List<string>();
        if (repos.Contains(PluginController.PLUGIN_BASE_URL) == false)
            repos.Add(PluginController.PLUGIN_BASE_URL);
        
        foreach(var repo in repos)
            Logger.Instance.ILog("Plugin repository: " + repo);
        
        var pluginDownloader = new PluginDownloader(repos);
        // download the plugins we just deleted, this ensures they have the UID set
        foreach (var plugin in plugins)
        {
            string name = plugin.Name;
            Logger.Instance.ILog("Updating plugin: " + name);
            // first delete it
            plugin.Delete();
            // now download it
            var result = pluginDownloader.Download(name);
            if (result.Success == false)
            {
                Logger.Instance.WLog("Failed updating plugin");
                continue;
            }

            File.WriteAllBytes(plugin.FullName, result.Data);
            Shared.Logger.Instance.ILog("Successfully downloaded updated plugin: " + plugin.Name);
        }
        
        // now scan the plugins again
        PluginScanner.Scan();
    }
}