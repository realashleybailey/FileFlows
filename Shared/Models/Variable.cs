namespace FileFlows.Shared.Models;

/// <summary>
/// A Variable used by the system
/// </summary>
public class Variable : FileFlowObject
{
    /// <summary>
    /// Gets or sets the value of the variable
    /// </summary>
    public string Value { get; set; }
}