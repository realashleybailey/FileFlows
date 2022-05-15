namespace FileFlows.Server.Services;

using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;

/// <summary>
/// Plugin service
/// </summary>
public class PluginService : IPluginService
{

    /// <summary>
    /// Download a plugin
    /// </summary>
    /// <param name="plugin">the plugin to download</param>
    /// <returns>the byte data of the plugin</returns>
    public Task<byte[]> Download(PluginInfo plugin)
    {
        var result = new PluginController().DownloadPackage(plugin.PackageName);
        using var ms = new MemoryStream();
        result.FileStream.CopyTo(ms);
        return Task.FromResult(ms.ToArray());
    }

    /// <summary>
    /// Get all plugin infos
    /// </summary>
    /// <returns>all plugin infos</returns>
    public Task<List<PluginInfo>> GetAll() => new PluginController().GetDataList();


    /// <summary>
    /// Gets the settings json for a plugin
    /// </summary>
    /// <param name="pluginPackageName">the name of the plugin package</param>
    /// <returns>the settings json</returns>
    public Task<string> GetSettingsJson(string pluginSettingsType) => new PluginController().GetPluginSettings(pluginSettingsType);


    /// <summary>
    /// Updates plugin info
    /// </summary>
    /// <param name="pluginInfo">the plugin info</param>
    /// <returns>the updated plugininfo</returns>
    public Task<PluginInfo> Update(PluginInfo pluginInfo) => new PluginController().Update(pluginInfo);
}