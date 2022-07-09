namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for open database connections
/// </summary>
public class OpenDatabaseConnections:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("82cac4a6-9bde-42df-9db0-9e4e67e8412d");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/system/history-data/database-connections";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-database";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Database Connections";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.TimeSeries;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 2;
}