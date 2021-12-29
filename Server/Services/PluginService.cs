namespace FileFlows.Server.Services
{
    using FileFlows.Plugin;
    using FileFlows.Server.Controllers;
    using FileFlows.Server.Helpers;
    using FileFlows.ServerShared.Helpers;
    using FileFlows.ServerShared.Services;
    using FileFlows.Shared.Models;


    public class PluginService : IPluginService
    {
        public async Task<byte[]> Download(PluginInfo plugin)
        {
            var result = await new PluginController().DownloadPackage(plugin.PackageName);
            using var ms = new MemoryStream();
            result.FileStream.CopyTo(ms);
            return ms.ToArray();
        }

        public Task<List<PluginInfo>> GetAll() => new PluginController().GetDataList();
        public Task<PluginInfo> Update(PluginInfo pluginInfo) => new PluginController().Update(pluginInfo);
    }
}
