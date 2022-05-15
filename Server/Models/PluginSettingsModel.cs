using FileFlows.Shared.Models;

namespace FileFlows.Server.Models;

/// <summary>
/// Plugin Settings Model
/// </summary>
internal class PluginSettingsModel : FileFlowObject
{
    /// <summary>
    /// Gets or sets the JSON for hte plugin settings
    /// </summary>
    public string Json { get; set; }
}