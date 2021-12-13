namespace FileFlows.ServerShared.Services
{
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared.Models;

    public interface INodeService
    {
        Task<ProcessingNode> Register(string address);

        Task<ProcessingNode> GetServerNode();

        Task<string> GetToolPath(string name);
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

        public Task<ProcessingNode> GetServerNode()
        {
            // not implemented here, since the Server INodeService will implement this
            throw new NotImplementedException();
        }

        public Task<string> GetToolPath(string name)
        {
            // todo 
            return Task.FromResult(@"C:\utils\ffmpeg\ffmpeg.exe");
        }

        public async Task<ProcessingNode> Register(string address)
        {
            var result = await HttpHelper.Get<ProcessingNode>(ServiceBaseUrl + "/node?address=" + Uri.EscapeDataString(address));
            if (result.Success == false)
                throw new Exception("Failed to register node: " + result.Body);
            return result.Data;
        }
    }
}

