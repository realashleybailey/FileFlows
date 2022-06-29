using FileFlows.Shared.Models;

namespace FileFlows.Shared.Portlets;

/// <summary>
/// Portlet for Memory Usage
/// </summary>
public class MemoryUsage:PortletDefinition
{
    /// <summary>
    /// The Portlet Definition UID
    /// </summary>
    internal static readonly Guid PD_UID = new ("8badf12b-0881-46da-a5f1-8cc7e0c64d5d");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => PD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/system/history-data/memory";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-memory";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Memory Usage";

    /// <summary>
    /// Gets the type of portlet
    /// </summary>
    public override PortletType Type => PortletType.TimeSeries;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 1;
}