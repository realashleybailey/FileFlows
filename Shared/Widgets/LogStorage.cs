using FileFlows.Shared.Models;

namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for Log Storage
/// </summary>
public class LogStorage:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("e7324e06-59fa-4923-8068-effcb9acba91");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

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
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.TimeSeries;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 1;
}