namespace FileFlows.Server.Services
{
    using FileFlows.Server.Controllers;
    using FileFlows.ServerShared.Services;
    using FileFlows.Shared.Models;


    public class PluginService : IPluginService
    {
        public Task<byte[]> Download(PluginInfo plugin)
        {
            var result = new PluginController().DownloadPackage(plugin.PackageName);
            using var ms = new MemoryStream();
            result.FileStream.CopyTo(ms);
            return Task.FromResult(ms.ToArray());
        }

        public Task<List<PluginInfo>> GetAll() => new PluginController().GetDataList();

        public Task<string> GetSettingsJson(string pluginSettingsType) => new PluginController().GetPluginSettings(pluginSettingsType);

        public Task<PluginInfo> Update(PluginInfo pluginInfo) => new PluginController().Update(pluginInfo);
    }
}
