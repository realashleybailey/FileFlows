namespace FileFlows.Shared.Models;

/// <summary>
/// A tool in FileFlows, e.g. an external application
/// </summary>
public class Tool : FileFlowObject
{
    /// <summary>
    /// Gets or sets the path of the tool
    /// </summary>
    public string Path { get; set; }
}