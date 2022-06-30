using FileFlows.Shared.Models;

namespace FileFlows.Shared.Portlets;

/// <summary>
/// Portlet for recently finished files
/// </summary>
public class FilesRecentlyFinished:PortletDefinition
{
    /// <summary>
    /// The Portlet Definition UID
    /// </summary>
    internal static readonly Guid PD_UID = new ("4c0b323c-7da1-4b05-b9bf-f07d3799e540");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => PD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/library-file/recently-finished";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Recently Finished";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-hourglass-end";

    /// <summary>
    /// Gets the type of portlet
    /// </summary>
    public override PortletType Type => PortletType.LibraryFileTable;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 1;
}