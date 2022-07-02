using FileFlows.Shared.Models;

namespace FileFlows.Shared.Portlets;

/// <summary>
/// Portlet for Codecs
/// </summary>
public class Codecs:PortletDefinition
{
    /// <summary>
    /// The Portlet Definition UID
    /// </summary>
    public static readonly Guid PD_UID = new ("d7f212b6-29eb-4912-b7b5-0efe1d377164");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => PD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/statistics/by-name/CODEC";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-photo-video";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Codecs";

    /// <summary>
    /// Gets the type of portlet
    /// </summary>
    public override PortletType Type => PortletType.TreeMap;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}