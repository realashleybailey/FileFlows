using FileFlows.Shared.Portlets;

namespace FileFlows.Shared.Models;

/// <summary>
/// UI Model for the portlet
/// </summary>
public class PortletUiModel
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
    /// Gets the type of portlet
    /// </summary>
    public PortletType Type { get; set; }
    /// <summary>
    /// Gets any flags 
    /// </summary>
    public int Flags { get; set; }
    
    /// <summary>
    /// Gets or sets the width of the portlet
    /// </summary>
    public int Width { get; set; }
    /// <summary>
    /// Gets or sets the height of the portlet
    /// </summary>
    public int Height { get; set; }
    /// <summary>
    /// Gets or sets the X position of the portlet
    /// </summary>
    public int X { get; set; }
    /// <summary>
    /// Gets or sets the Y position of the portlet
    /// </summary>
    public int Y { get; set; }
}