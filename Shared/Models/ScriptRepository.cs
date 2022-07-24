namespace FileFlows.Shared.Models;

/// <summary>
/// The script repository
/// </summary>
public class ScriptRepository
{
    /// Gets or sets the shared scripts
    /// </summary>
    public List<RepositoryScript> SharedScripts { get; set; } = new ();
    /// <summary>
    /// Gets or sets the system scripts
    /// </summary>
    public List<RepositoryScript> SystemScripts { get; set; } = new ();
    /// <summary>
    /// Gets or sets the flow scripts
    /// </summary>
    public List<RepositoryScript> FlowScripts { get; set; } = new ();
}

/// <summary>
/// A Remote script
/// </summary>
public class RepositoryScript
{
    /// <summary>
    /// Gets or sets the path of the script
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the revision of the script
    /// </summary>
    public int Revision { get; set; }
}