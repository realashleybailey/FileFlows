namespace FileFlows.Shared;

using FileFlows.Plugin;

/// <summary>
/// A logger used to write log messages 
/// </summary>
public class Logger
{
    /// <summary>
    /// Gets or sets the instance of ILogger
    /// </summary>
    public static ILogger Instance { get; set; }
}