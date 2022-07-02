using FileFlows.Shared.Models;

namespace FileFlows.Shared.Portlets;

/// <summary>
/// Portlet for Log Storage
/// </summary>
public class LogStorage:PortletDefinition
{
    /// <summary>
    /// The Portlet Definition UID
    /// </summary>
    public static readonly Guid PD_UID = new ("e7324e06-59fa-4923-8068-effcb9acba91");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => PD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/system/history-data/log-storage";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-hdd";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Log Storage";

    /// <summary>
    /// Gets the type of portlet
    /// </summary>
    public override PortletType Type => PortletType.TimeSeries;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 1;
}