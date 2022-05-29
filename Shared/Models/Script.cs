namespace FileFlows.Shared.Models;

/// <summary>
/// A script is a special function node that lets you reuse them
/// </summary>
public class Script:FileFlowObject
{
    /// <summary>
    /// Gets or sets the description of the script
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the author of the script
    /// </summary>
    public string Author { get; set; }
    
    /// <summary>
    /// Gets or sets the version of the script
    /// </summary>
    public string Version { get; set; }
    
    /// <summary>
    /// Gets or sets the javascript code of the script
    /// </summary>
    public string Code { get; set; }
    
    /// <summary>
    /// Gets or sets the output connections this script can have
    /// </summary>
    public ScriptOutput[] Outputs { get; set; }
}

/// <summary>
/// Defines a script output connection
/// </summary>
public class ScriptOutput 
{
    public int Output { get; set; }
    public string Description { get; set; }
}