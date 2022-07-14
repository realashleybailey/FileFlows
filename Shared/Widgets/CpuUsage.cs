using FileFlows.Shared.Models;

namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for CPU Usage
/// </summary>
public class CpuUsage:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("1abd867e-fe34-4832-bb59-1236c3960855");

    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/system/history-data/cpu";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-microchip";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "CPU Usage";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.TimeSeries;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}