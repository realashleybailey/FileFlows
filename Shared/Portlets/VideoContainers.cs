using FileFlows.Shared.Models;

namespace FileFlows.Shared.Portlets;

/// <summary>
/// Portlet for Video Containres
/// </summary>
public class VideoContainers:PortletDefinition
{
    /// <summary>
    /// The Portlet Definition UID
    /// </summary>
    internal static readonly Guid PD_UID = new ("eca4b7ed-97bd-4947-bd30-9307abba7900");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => PD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/by-name/VIDEO_CONTAINER";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-file-video";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Video Containers";

    /// <summary>
    /// Gets the type of portlet
    /// </summary>
    public override PortletType Type => PortletType.PieChart;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}