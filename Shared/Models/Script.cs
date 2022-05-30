namespace FileFlows.Shared.Models;

/// <summary>
/// A script is a special function node that lets you reuse them
/// </summary>
public class Script:FileFlowObject
{   
    /// <summary>
    /// Gets or sets the javascript code of the script
    /// </summary>
    public string Code { get; set; }
}