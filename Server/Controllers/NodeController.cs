namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Helpers;

    [Route("/api/node")]
    public class NodeController : ControllerStore<ProcessingNode>
    {
#if(DEBUG)
        internal static string SignalrUrl = "http://localhost:6868/flow";
#else
        internal static string SignalrUrl = "http://localhost:5000/flow";
#endif

        [HttpGet]
        public async Task<IEnumerable<ProcessingNode>> GetAll() => (await GetDataList()).OrderBy(x => x.Address == Globals.FileFlowsServer ? 0 : 1).ThenBy(x => x.Name);

        [HttpGet("{uid}")]
        public Task<ProcessingNode> Get(Guid uid) => GetByUid(uid);

        [HttpPost]
        public async Task<ProcessingNode> Save([FromBody] ProcessingNode node)
        {
            // see if we are updating the internal node
            if(node.Address  == Globals.FileFlowsServer)
            {
                var internalNode = (await GetAll()).Where(x => x.Address == Globals.FileFlowsServer).FirstOrDefault();
                if(internalNode != null)
                {
                    internalNode.Schedule = node.Schedule;
                    internalNode.FlowRunners = node.FlowRunners;
                    internalNode.Enabled = node.Enabled;
                    internalNode.TempPath = node.TempPath;
                    return await Update(internalNode, checkDuplicateName: true);
                }
                else
                {
                    // internal but doesnt exist
                    node.Address = Globals.FileFlowsServer;
                    node.Name = Globals.FileFlowsServer;
                    node.Mappings = null; // no mappings for internal
                }
            }
            return await Update(node, checkDuplicateName: true);
        }

        [HttpDelete]
        public async Task Delete([FromBody] ReferenceModel model)
        {
            var internalNode = (await this.GetAll()).Where(x => x.Address == Globals.FileFlowsServer).FirstOrDefault()?.Uid ?? Guid.Empty;
            if (model.Uids.Contains(internalNode))
                throw new Exception("ErrorMessages.CannotDeleteInternalNode");
            await DeleteAll(model);
        }

        [HttpPut("state/{uid}")]
        public async Task<ProcessingNode> SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
        {
            var node = await GetByUid(uid);
            if (node == null)
                throw new Exception("Node not found.");
            if (enable != null)
            {
                node.Enabled = enable.Value;
                await DbManager.Update(node);
            }
            return node;
        }


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
            var tools = await new ToolController().GetAll();
            bool isSystem = address == Globals.FileFlowsServer;
            var result = await Update(new ProcessingNode
            {
                Name = address,
                Address = address,
                Enabled = isSystem, // default to disabled so they have to configure it first
                FlowRunners = 1,
                Schedule = new string('1', 672),
                Mappings = isSystem  ? null : tools.Select(x => new
                    KeyValuePair<string, string>(x.Path, "")
                ).ToList()
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
                bool windows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
                node = await Update(new ProcessingNode
                {
                    Name = Globals.FileFlowsServer,
                    Address = Globals.FileFlowsServer,
                    Schedule = new string('1', 672),
                    Enabled = true,
                    FlowRunners = 1,
#if (DEBUG)
                    TempPath = windows ? @"d:\videos\temp" : "/temp",
#else
                    TempPath = windows ? @"d:\videos\temp" : "/temp",
#endif
                });
            }
            node.SignalrUrl = SignalrUrl;
            return node;
        }
    }

}