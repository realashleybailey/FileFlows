namespace FileFlows.ServerShared.Services
{
    using FileFlows.Plugin;
    using FileFlows.ServerShared.Helpers;
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared.Models;

    public interface IPluginService
    {

        Task<List<PluginInfo>> GetAll();
        Task<PluginInfo> Update(PluginInfo pluginInfo);
        Task<Node> LoadNode(FlowPart part);
    }

    public class PluginService : Service, IPluginService
    {

        public static Func<IPluginService> Loader { get; set; }

        public static IPluginService Load()
        {
            if (Loader == null)
                return new PluginService();
            return Loader.Invoke();
        }

        public async Task<List<PluginInfo>> GetAll()
        {
            try
            {
                var result = await HttpHelper.Get<List<PluginInfo>>($"{ServiceBaseUrl}/api/plugin");
                if (result.Success == false)
                    throw new Exception("Failed to load plugin infos: " + result.Body);
                return result.Data;
            }
            catch (Exception ex)
            {
                Logger.Instance?.WLog("Failed to get plugin infos: " + ex.Message);
                return new List<PluginInfo>();
            }
        }

        public Task<Node> LoadNode(FlowPart part)
        {
            using var pluginLoader = new PluginHelper();
            return Task.FromResult(pluginLoader.LoadNode(part));
        }

        public async Task<PluginInfo> Update(PluginInfo pluginInfo)
        {
            throw new NotImplementedException();
        }
    }
}
