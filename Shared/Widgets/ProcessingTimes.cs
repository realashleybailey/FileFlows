using FileFlows.Shared.Models;

namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Processing Times
/// </summary>
public class ProcessingTimes:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("3041621f-8756-4e91-8b93-215f028b9454");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

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
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.HeatMap;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}