namespace FileFlows.Server.Services;

using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// An Service for communicating with the server for all Processing Node related actions
/// </summary>
public class NodeService : INodeService
{
    /// <summary>
    /// Clears all workers on the node.
    /// This is called when a node first starts up, if a node crashed when workers were running this will reset them
    /// </summary>
    /// <param name="nodeUid">The UID of the node</param>
    /// <returns>a completed task</returns>
    public Task ClearWorkers(Guid nodeUid) => new WorkerController(null).Clear(nodeUid);

    /// <summary>
    /// Gets an instance of the internal processing node
    /// </summary>
    /// <returns>an instance of the internal processing node</returns>
    public Task<ProcessingNode> GetServerNode() => new NodeController().GetServerNode();

    /// <summary>
    /// Gets a tool path by name
    /// </summary>
    /// <param name="name">The name of the tool</param>
    /// <returns>a tool path</returns>
    public async Task<string> GetToolPath(string name)
    {
        var result = await new ToolController().GetByName(name);
        return result?.Path ?? string.Empty;
    }

    /// <summary>
    /// Gets a processing node by its physical address
    /// </summary>
    /// <param name="address">The address (hostname or IP address) of the node</param>
    /// <returns>An instance of the processing node</returns>
    public async Task<ProcessingNode> GetByAddress(string address)
    {
        var result = await new NodeController().GetByAddress(address, Globals.Version);
        result.SignalrUrl = $"http://localhost:{WebServer.Port}/flow";
        return result;
    }
}