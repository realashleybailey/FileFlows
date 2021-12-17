namespace FileFlows.ServerShared.Services
{
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared.Models;

    public interface INodeService
    {
        Task<ProcessingNode> GetByAddress(string address);

        Task<ProcessingNode> GetServerNode();

        Task<string> GetToolPath(string name);

        Task ClearWorkers(Guid nodeUid);
    }

    public class NodeService : Service, INodeService
    {

        public static Func<INodeService> Loader { get; set; }

        public static INodeService Load()
        {
            if (Loader == null)
                return new NodeService();
            return Loader.Invoke();
        }

        public async Task ClearWorkers(Guid nodeUid)
        {
            try
            {
                await HttpHelper.Post(ServiceBaseUrl + "/api/worker/clear/" + Uri.EscapeDataString(nodeUid.ToString()));
            }
            catch (Exception)
            {
                return;
            }
        }

        public Task<ProcessingNode> GetServerNode()
        {
            // not implemented here, since the Server INodeService will implement this
            throw new NotImplementedException();
        }

        public async Task<string> GetToolPath(string name)
        {
            try
            {
                var result = await HttpHelper.Get<Tool>(ServiceBaseUrl + "/api/tool/name/" + Uri.EscapeDataString(name));
                return result.Data.Path;
            }
            catch (Exception ex)
            {
                Logger.Instance?.ELog("Failed to locate tool: " + name + " => " + ex.Message);
                return string.Empty;
            }
        }

        public async Task<ProcessingNode> GetByAddress(string address)
        {            
            var result = await HttpHelper.Get<ProcessingNode>(ServiceBaseUrl + "/api/node/by-address/" + Uri.EscapeDataString(address));
            if (result.Success == false)
                throw new Exception("Failed to register node: " + result.Body);
            result.Data.SignalrUrl = ServiceBaseUrl + "/flow";
            return result.Data;
        }
        public async Task<ProcessingNode> Register(string serverUrl, string address, string tempPath, int runners, bool enabled)
        {
            if(serverUrl.EndsWith("/"))
                serverUrl = serverUrl.Substring(0, serverUrl.Length - 1);

            var result = await HttpHelper.Post<ProcessingNode>(serverUrl + "/api/node/register", new 
            {
                Address = address,
                TempPath = tempPath,
                FlowRunners = runners,
                Enabled = enabled
            }, timeoutSeconds: 15);

            if (result.Success == false)
                throw new Exception("Failed to register node: " + result.Body);

            return result.Data;
        }
    }
}

