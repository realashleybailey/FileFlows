using FileFlows.Shared.Models;

namespace FileFlows.Shared.Portlets;

/// <summary>
/// Portlet for Temporary File Storage
/// </summary>
public class TempStorage:PortletDefinition
{
    /// <summary>
    /// The Portlet Definition UID
    /// </summary>
    internal static readonly Guid PD_UID = new ("40f8b3d8-e267-45f1-855f-a4ad9b13fac1");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => PD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/system/history-data/temp-storage";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-hdd";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Temporary File Storage";

    /// <summary>
    /// Gets the type of portlet
    /// </summary>
    public override PortletType Type => PortletType.TimeSeries;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 1;
}