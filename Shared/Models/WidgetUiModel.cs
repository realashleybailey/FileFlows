using FileFlows.Shared.Widgets;

namespace FileFlows.Shared.Models;

/// <summary>
/// UI Model for the widget
/// </summary>
public class WidgetUiModel
{
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public Guid Uid { get; set; }
    /// <summary>
    /// Gets the URL
    /// </summary>
    public string Url { get; set; }
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public string Icon { get; set; }
    /// <summary>
    /// Gets the Name
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets the type of widget
    /// </summary>
    public WidgetType Type { get; set; }
    /// <summary>
    /// Gets any flags 
    /// </summary>
    public int Flags { get; set; }
    
    /// <summary>
    /// Gets or sets the width of the widget
    /// </summary>
    public int Width { get; set; }
    /// <summary>
    /// Gets or sets the height of the widget
    /// </summary>
    public int Height { get; set; }
    /// <summary>
    /// Gets or sets the X position of the widget
    /// </summary>
    public int X { get; set; }
    /// <summary>
    /// Gets or sets the Y position of the widget
    /// </summary>
    public int Y { get; set; }
}