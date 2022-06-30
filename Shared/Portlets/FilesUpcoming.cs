using FileFlows.Shared.Models;

namespace FileFlows.Shared.Portlets;

/// <summary>
/// Portlet for upcoming files
/// </summary>
public class FilesUpcoming:PortletDefinition
{
    /// <summary>
    /// The Portlet Definition UID
    /// </summary>
    internal static readonly Guid PD_UID = new ("1a545039-e37f-43a7-a3db-2b0640d83905");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => PD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/library-file/upcoming";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Upcoming";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-hourglass-start";

    /// <summary>
    /// Gets the type of portlet
    /// </summary>
    public override PortletType Type => PortletType.LibraryFileTable;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}