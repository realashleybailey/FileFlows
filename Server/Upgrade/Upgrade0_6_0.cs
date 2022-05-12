using FileFlows.Server.Controllers;

namespace FileFlows.Server.Upgrade;

public class Upgrade0_6_0
{
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

            node.AllLibraries = true;
            node.MaxFileSizeMb = 0;
            
            Logger.Instance.ILog("Upgrading Node: " + node.Name);
            controller.Update(node).Wait();
            ++count;
        }
        Logger.Instance.ILog($"Upgraded {count} node{(count == 1 ? "" : "s")}");
    }
}
