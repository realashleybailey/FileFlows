namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Helpers;

    [Route("/api/node")]
    public class NodeController : ControllerStore<ProcessingNode>
    {
        internal static string SignalrUrl = "http://localhost:6868/flow";

        [HttpGet]
        public async Task<IEnumerable<ProcessingNode>> GetAll() => (await GetDataList()).OrderBy(x => x.Address == Globals.FileFlowsServer ? 0 : 1).ThenBy(x => x.Name);

        [HttpGet("{uid}")]
        public Task<ProcessingNode> Get(Guid uid) => GetByUid(uid);

        [HttpPost]
        public Task<ProcessingNode> Save([FromBody] ProcessingNode node) => Update(node, checkDuplicateName: true);


        [HttpGet("register")]
        public async Task<ProcessingNode> Register([FromQuery]string address)
        {
            if(string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(nameof(address));

            address = address.Trim();
            var data = await GetData();
            var existing = data.Where(x => x.Value.Address.ToLower() == address.ToLower()).Select(x => x.Value).FirstOrDefault();
            if (existing != null)
            {
                existing.SignalrUrl = SignalrUrl;
                return existing;
            }
            var settings = await new SettingsController().Get();
            // doesnt exist, register a new node.
            var result = await Update(new ProcessingNode
            {
                Name = address,
                Address = address,
                TempPath = settings.TempPath,
                Enabled = true,
                FlowRunners = settings.FlowRunners
            });
            result.SignalrUrl = SignalrUrl;
            return result;
        }

        internal async Task<ProcessingNode> GetServerNode()
        {
            var data = await GetData();
            var settings = await new SettingsController().Get();
            var node = data.Where(x => x.Value.Name == Globals.FileFlowsServer).Select(x => x.Value).FirstOrDefault();
            if (node == null)
            {
                node = await Update(new ProcessingNode
                {
                    Name = Globals.FileFlowsServer,
                    Address = Globals.FileFlowsServer
                });
            }
            node.FlowRunners = settings.FlowRunners;
            node.Enabled = settings.WorkerFlowExecutor;
            node.TempPath = settings.TempPath;
            node.SignalrUrl = SignalrUrl;
            return node;
        }
    }

}