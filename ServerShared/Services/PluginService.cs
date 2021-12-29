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

        Task<byte[]> Download(PluginInfo plugin);
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

        public async Task<byte[]> Download(PluginInfo plugin)
        {
            try
            {
                var result = await HttpHelper.Post<byte[]>($"{ServiceBaseUrl}/api/plugin/download-package", plugin);
                if (result.Success == false)
                    throw new Exception("Failed to download plugin package: " + result.Body);
                return result.Data;
            }
            catch (Exception ex)
            {
                Logger.Instance?.WLog("Failed to get plugin infos: " + ex.Message);
                return new byte[] { };
            }
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

        public async Task<PluginInfo> Update(PluginInfo pluginInfo)
        {
            throw new NotImplementedException();
        }
    }
}
