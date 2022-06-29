using FileFlows.Shared.Portlets;

namespace FileFlows.Shared.Models;

/// <summary>
/// Dashboard that shows portlets
/// </summary>
public class Dashboard: FileFlowObject
{
    private List<Portlet> _Portlets = new List<Portlet>();

    /// <summary>
    /// The name of the default dashboard
    /// </summary>
    public const string DefaultDashboardName = "Default Dashboard";
    
    /// <summary>
    /// The UID of the default dashboard
    /// </summary>
    public static readonly Guid DefaultDashboardUid = new Guid("bed286d9-68f0-48a8-8c6d-05ec6f81d67c");
        
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

    /// <summary>
    /// Gets a default dashboard
    /// </summary>
    /// <returns>the default dashboard</returns>
    public static Dashboard GetDefaultDashboard()
    {
        var db = new Dashboard();
        db.Name = DefaultDashboardName;
        db.Uid = DefaultDashboardUid;
        db.Portlets = new();
        // top row
        db.Portlets.Add(new Portlet()
        {
            Height = 1, Width = 3,
            Y = 0, X = 0,
            PortletDefinitionUid = new CpuUsage().Uid
        });
        db.Portlets.Add(new Portlet()
        {
            Height = 1, Width = 3,
            Y = 0, X = 3,
            PortletDefinitionUid = new MemoryUsage().Uid
        });
        db.Portlets.Add(new Portlet()
        {
            Height = 1, Width = 3,
            Y = 0, X = 6,
            PortletDefinitionUid = new TempStorage().Uid
        });
        db.Portlets.Add(new Portlet()
        {
            Height = 1, Width = 3,
            Y = 0, X = 9,
            PortletDefinitionUid = new LogStorage().Uid
        });
        
        // second row
        db.Portlets.Add(new Portlet()
        {
            Height = 2, Width = 6,
            Y = 1, X = 0,
            PortletDefinitionUid = new Codecs().Uid
        });
        db.Portlets.Add(new Portlet()
        {
            Height = 2, Width = 6,
            Y = 1, X = 6,
            PortletDefinitionUid = new ProcessingTimes().Uid
        });
        
        // bottom row
        db.Portlets.Add(new Portlet()
        {
            Height = 2, Width = 4,
            Y = 3, X = 0,
            PortletDefinitionUid = new VideoContainers().Uid
        });
        db.Portlets.Add(new Portlet()
        {
            Height = 2, Width = 4,
            Y = 3, X = 4,
            PortletDefinitionUid = new VideoResolution().Uid
        });
        db.Portlets.Add(new Portlet()
        {
            Height = 2, Width = 4,
            Y = 3, X = 8,
            PortletDefinitionUid = new LibraryProcessingTimes().Uid
        });

        return db;
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