namespace FileFlows.Shared.Models;

public class PluginPackageInfo
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string Authors { get; set; }
    public string Url { get; set; }
    public string Description { get; set; }
    public string Package { get; set; }
    public string MinimumVersion { get; set; }
    public string[] Elements { get; set; }
}
