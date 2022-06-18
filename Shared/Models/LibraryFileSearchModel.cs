using FileFlows.Plugin;

namespace FileFlows.Shared.Models;


/// <summary>
/// A model used to search the library files
/// </summary>
public class LibraryFileSearchModel
{
    /// <summary>
    /// Gets or sets the path to search for
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets the from date to search
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Gets or sets the to date to search
    /// </summary>
    public DateTime ToDate { get; set; }
}