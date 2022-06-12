using FileFlows.Server.Controllers;
using FileFlows.Shared;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v0.6.0
/// </summary>
public class Upgrade0_6_0
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run()
    {
        Logger.Instance.ILog("Upgrade running, running 0.6.0 upgrade script");

        var controller = new NodeController();
        var nodes = controller.GetAll().Result;
        int count = 0;
        foreach (var node in nodes)
        {
            if (node.Mappings?.Any() == true)
            {
                bool update = false;
                var newMappings = new List<KeyValuePair<string, string>>();
                foreach (var mapping in node.Mappings)
                {
                    if (mapping.Value.Contains("Node\\Tools\\"))
                    {
                        newMappings.Add(new KeyValuePair<string, string>(mapping.Key,
                            mapping.Value.Replace("Node\\Tools\\", "Tools\\")));
                        update = true;
                    }
                    else
                    {
                        newMappings.Add(mapping);
                    }
                }
                if(update)
                    node.Mappings = newMappings;
            }

            node.AllLibraries = ProcessingLibraries.All;
            node.MaxFileSizeMb = 0;
            
            Logger.Instance.ILog("Upgrading Node: " + node.Name);
            controller.Update(node).Wait();
            ++count;
        }
        Logger.Instance.ILog($"Upgraded {count} node{(count == 1 ? "" : "s")}");
    }
}
