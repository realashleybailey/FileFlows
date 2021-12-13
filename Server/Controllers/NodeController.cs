namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Helpers;

    [Route("/api/node")]
    public class NodeController : ControllerStore<ProcessingNode>
    {
        [HttpGet]
        public async Task<ProcessingNode> Register([FromQuery]string address)
        {
            if(string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(nameof(address));

            address = address.Trim();
            var data = await GetData();
            var existing = data.Where(x => x.Value.Name.ToLower() == address.ToLower()).Select(x => x.Value).FirstOrDefault();
            if (existing != null)
                return existing;
            // doesnt exist, register a new node.
            return await Update(new ProcessingNode
            {
                Name = address
            });
        }

        internal async Task<ProcessingNode> GetServerNode()
        {
            var data = await GetData();
            var existing = data.Where(x => x.Value.Name == "FileFlowsServer").Select(x => x.Value).FirstOrDefault();
            if (existing != null)
                return existing;
            return await Update(new ProcessingNode
            {
                Name = "FileFlowsServer"
            });
        }
    }

}