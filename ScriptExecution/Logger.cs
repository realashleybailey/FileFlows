namespace FileFlows.ScriptExecution;

/// <summary>
/// Logger for the script executor
/// </summary>
public class Logger
{
    /// <summary>
    /// Constructs a logger instance
    /// </summary>
    public Logger()
    {
    }

    /// <summary>
    /// Gets or sets error info action
    /// </summary>
    public Action<object[]> ILogAction { get; set; }
    /// <summary>
    /// Gets or sets debug log action
    /// </summary>
    public Action<object[]> DLogAction { get; set; }
    /// <summary>
    /// Gets or sets warning log action
    /// </summary>
    public Action<object[]> WLogAction { get; set; }
    /// <summary>
    /// Gets or sets error log action
    /// </summary>
    public Action<object[]> ELogAction { get; set; }

    /// <summary>
    /// Logs a info message
    /// </summary>
    /// <param name="args">the args to log</param>
    public void ILog(params object[] args) => ILogAction?.Invoke(args);
    
    /// <summary>
    /// Logs a debug message
    /// </summary>
    /// <param name="args">the args to log</param>
    public void DLog(params object[] args) => DLogAction?.Invoke(args);
    
    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="args">the args to log</param>
    public void WLog(params object[] args) => WLogAction?.Invoke(args);
    
    /// <summary>
    /// Logs a error message
    /// </summary>
    /// <param name="args">the args to log</param>
    public void ELog(params object[] args) => ELogAction?.Invoke(args);
}