using FileFlows.Shared.Models;

namespace FileFlows.Shared.Portlets;

/// <summary>
/// Portlet for Library Processing Times
/// </summary>
public class LibraryProcessingTimes:PortletDefinition
{
    /// <summary>
    /// The Portlet Definition UID
    /// </summary>
    internal static readonly Guid PD_UID = new ("356b405b-5fc2-44e6-84dd-7c04c0894c9c");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => PD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/system/history-data/library-processing-time";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-stopwatch";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Library Processing Times";

    /// <summary>
    /// Gets the type of portlet
    /// </summary>
    public override PortletType Type => PortletType.BoxPlot;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}