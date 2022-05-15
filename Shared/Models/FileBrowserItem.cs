namespace FileFlows.Shared.Models;

/// <summary>
/// An item in the file browser
/// </summary>
public class FileBrowserItem
{
    /// <summary>
    /// Gets or sets the short name of an item in the file browser
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the fullname of the item
    /// </summary>
    public string FullName { get; set; }
    
    /// <summary>
    /// Gets or sets if the item is a path
    /// </summary>
    public bool IsPath { get; set; }

    /// <summary>
    /// Gets or sets if the item is a parent item
    /// </summary>
    public bool IsParent { get; set; }

    /// <summary>
    /// Gets or sets if the item is a drive
    /// </summary>
    public bool IsDrive { get; set; }
}