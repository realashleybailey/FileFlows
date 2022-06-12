using FileFlows.Server.Controllers;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v0.5.3
/// </summary>
public class Upgrade0_5_3
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run()
    {
        Logger.Instance.ILog("Upgrade running, running 0.5.3 upgrade script");

        var controller = new FlowController();
        var flows = controller.GetAll().Result;
        int count = 0;
        foreach (var flow in flows) 
        {
            if (flow?.Parts?.Any() != true)
                continue;
            bool update = false;

            foreach(var node in flow.Parts)
            {
                if(node.FlowElementUid == "BasicNodes.Tools.WebRequest")
                {
                    node.FlowElementUid = "FileFlows." + node.FlowElementUid;
                    update = true;
                }
            }
            if (update)
            {
                Logger.Instance.ILog("Upgrading Flow: " + flow.Name);
                controller.Update(flow).Wait();
                ++count;
            }
        }
        Logger.Instance.ILog($"Upgraded {count} flow{(count == 1 ? "" : "s")}");
    }
}
