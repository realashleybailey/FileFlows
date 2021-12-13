namespace FileFlows.Server.Services
{
    using FileFlows.Server.Controllers;
    using FileFlows.ServerShared.Services;
    using FileFlows.Shared.Models;
    using System.Threading.Tasks;

    public class NodeService : INodeService
    {
        public Task<ProcessingNode> GetServerNode() => new NodeController().GetServerNode();

        public Task<ProcessingNode> Register(string address) => new NodeController().Register(address);
    }
}
