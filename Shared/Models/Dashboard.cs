namespace FileFlows.Shared.Models;

/// <summary>
/// Dashboard that shows portlets
/// </summary>
public class Dashboard: FileFlowObject
{
    private List<Portlet> _Portlets = new List<Portlet>();

    /// <summary>
    /// Gets or sets the portlets on this dashboard
    /// </summary>
    public List<Portlet> Portlets
    {
        get => _Portlets;
        set
        {
            _Portlets.Clear();
            if(value != null)
                _Portlets.AddRange(value);
        }
    }
}

/// <summary>
/// Portlet shown on a dashboard
/// </summary>
public class Portlet
{
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

    /// <summary>
    /// The UID of the Portlet Definition
    /// </summary>
    public Guid PortletDefinitionUid { get; set; }
}

/// <summary>
/// Portlet definition, these are the different types of portlets in the system
/// </summary>
public class PortletDefinition
{
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public Guid Uid { get; }
    /// <summary>
    /// Gets the URL
    /// </summary>
    public string Url { get; }
    /// <summary>
    /// Gets the Name
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Gets the type of portlet
    /// </summary>
    public PortletType Type { get; }
    /// <summary>
    /// Gets any flags 
    /// </summary>
    public int Flags { get; }
}


/// <summary>
/// Available portlet types
/// </summary>
public enum PortletType
{
    /// <summary>
    /// Processing files
    /// </summary>
    Processing = 1,
    /// <summary>
    /// Upcoming videos
    /// </summary>
    Upcoming = 2,
    /// <summary>
    /// Recently finished
    /// </summary>
    RecentlyFinished = 1,
    /// <summary>
    /// Box plot 
    /// </summary>
    BoxPlot = 101,
    /// <summary>
    /// Heat map
    /// </summary>
    HeatMap = 102,
    /// <summary>
    /// Pie chart
    /// </summary>
    PieChart = 103,
    /// <summary>
    /// Tree map
    /// </summary>
    TreeMap = 104,
    /// <summary>
    /// Time series percentage
    /// </summary>
    TimeSeries = 105
}