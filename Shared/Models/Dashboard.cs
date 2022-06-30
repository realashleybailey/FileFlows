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
        int rowIndex = 0;
        
        // top row
        db.Portlets.Add(new ()
        {
            Height = 1, Width = 3,
            Y = rowIndex, X = 0,
            PortletDefinitionUid = CpuUsage.PD_UID
        });
        db.Portlets.Add(new ()
        {
            Height = 1, Width = 3,
            Y = rowIndex, X = 3,
            PortletDefinitionUid = MemoryUsage.PD_UID
        });
        db.Portlets.Add(new ()
        {
            Height = 1, Width = 3,
            Y = rowIndex, X = 6,
            PortletDefinitionUid = TempStorage.PD_UID
        });
        db.Portlets.Add(new ()
        {
            Height = 1, Width = 3,
            Y = rowIndex, X = 9,
            PortletDefinitionUid = LogStorage.PD_UID
        });
        ++rowIndex;
        
        // executing
        db.Portlets.Add(new ()
        {
            Height = 2, Width = 12,
            Y = rowIndex, X = 0,
            PortletDefinitionUid = Processing.PD_UID
        });
        rowIndex += 2;
        
        // library files row
        db.Portlets.Add(new ()
        {
            Height = 2, Width = 6,
            Y = rowIndex, X = 0,
            PortletDefinitionUid = FilesUpcoming.PD_UID
        });
        db.Portlets.Add(new ()
        {
            Height = 2, Width = 6,
            Y = rowIndex, X = 6,
            PortletDefinitionUid = FilesRecentlyFinished.PD_UID
        });
        rowIndex += 2;
        
        
        
        // codecs and times row
        db.Portlets.Add(new ()
        {
            Height = 2, Width = 6,
            Y = rowIndex, X = 0,
            PortletDefinitionUid = Codecs.PD_UID
        });
        db.Portlets.Add(new ()
        {
            Height = 2, Width = 6,
            Y = rowIndex, X = 6,
            PortletDefinitionUid = ProcessingTimes.PD_UID
        });
        rowIndex += 2;
        
        // containers, resolution, processing times row
        db.Portlets.Add(new ()
        {
            Height = 2, Width = 4,
            Y = rowIndex, X = 0,
            PortletDefinitionUid = VideoContainers.PD_UID
        });
        db.Portlets.Add(new ()
        {
            Height = 2, Width = 4,
            Y = rowIndex, X = 4,
            PortletDefinitionUid = VideoResolution.PD_UID
        });
        db.Portlets.Add(new ()
        {
            Height = 2, Width = 4,
            Y = rowIndex, X = 8,
            PortletDefinitionUid = LibraryProcessingTimes.PD_UID
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