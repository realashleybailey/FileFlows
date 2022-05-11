namespace FileFlows.Server.Services
{
    using FileFlows.Server.Controllers;
    using FileFlows.ServerShared.Services;
    using FileFlows.Shared.Models;
    using System;
    using System.Threading.Tasks;

    public class NodeService : INodeService
    {
        public Task ClearWorkers(Guid nodeUid) => new WorkerController(null).Clear(nodeUid);

        public Task<ProcessingNode> GetServerNode() => new NodeController().GetServerNode();

        public async Task<string> GetToolPath(string name)
        {
            var result = await new ToolController().GetByName(name);
            return result?.Path ?? string.Empty;
        }

        public async Task<ProcessingNode> GetByAddress(string address)
        {
            var result = await new NodeController().GetByAddress(address, Globals.Version);
            result.SignalrUrl = $"http://localhost:{WebServer.Port}/flow";
            return result;
        }
    }
}
