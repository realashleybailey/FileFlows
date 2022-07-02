using FileFlows.Shared.Models;

namespace FileFlows.Shared.Portlets;

/// <summary>
/// Portlet for Video Codecs
/// </summary>
public class VideoCodecs:PortletDefinition
{
    /// <summary>
    /// The Portlet Definition UID
    /// </summary>
    public static readonly Guid PD_UID = new ("e4ea40ed-f52b-4f81-bded-9eb402192b14");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => PD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/by-name/VIDEO_CODEC";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-video";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Video Codecs";

    /// <summary>
    /// Gets the type of portlet
    /// </summary>
    public override PortletType Type => PortletType.TreeMap;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}