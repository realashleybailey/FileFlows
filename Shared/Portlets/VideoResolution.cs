using FileFlows.Shared.Models;

namespace FileFlows.Shared.Portlets;

/// <summary>
/// Portlet for Video Resolution
/// </summary>
public class VideoResolution:PortletDefinition
{
    /// <summary>
    /// The Portlet Definition UID
    /// </summary>
    public static readonly Guid PD_UID = new ("af4a5687-18f5-406b-aa96-abb45912f289");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => PD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/by-name/VIDEO_RESOLUTION";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Video Resolutions";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-tv";

    /// <summary>
    /// Gets the type of portlet
    /// </summary>
    public override PortletType Type => PortletType.PieChart;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}