using FileFlows.Shared.Models;

namespace FileFlows.Shared.Portlets;

/// <summary>
/// Portlet for Processing Times
/// </summary>
public class ProcessingTimes:PortletDefinition
{
    /// <summary>
    /// The Portlet Definition UID
    /// </summary>
    public static readonly Guid PD_UID = new ("3041621f-8756-4e91-8b93-215f028b9454");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => PD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/system/history-data/processing-heatmap";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-calendar-alt";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Processing Times";

    /// <summary>
    /// Gets the type of portlet
    /// </summary>
    public override PortletType Type => PortletType.HeatMap;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}