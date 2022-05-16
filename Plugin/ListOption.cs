namespace FileFlows.Plugin;

/// <summary>
/// An option that appears it UI list
/// </summary>
public class ListOption
{
    /// <summary>
    /// Gets or sets the display label for the option
    /// </summary>
    public string? Label { get; set; }
    
    /// <summary>
    /// Gets or sets the value for the option
    /// </summary>
    public object? Value { get; set; }
}
