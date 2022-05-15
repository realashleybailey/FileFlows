namespace FileFlows.ServerShared;

/// <summary>
/// A logger that outputs to the console
/// </summary>
public class Logger : Plugin.ILogger
{
    private enum LogType { Error, Warning, Debug, Info }
    private void Log(LogType type, object[] args)
    {
        if (type == LogType.Debug)
        {
            Console.WriteLine(type + " -> " + string.Join(", ", args.Select(x =>
                x == null ? "null" :
                x.GetType().IsPrimitive ? x.ToString() :
                x is string ? x.ToString() :
                System.Text.Json.JsonSerializer.Serialize(x)))
            );
        }
    }

    /// <summary>
    /// Logs an information message
    /// </summary>
    /// <param name="args">Any arguments for the log message</param>
    public void ILog(params object[] args) => Log(LogType.Info, args);
    /// <summary>
    /// Logs an debug message
    /// </summary>
    /// <param name="args">Any arguments for the log message</param>
    public void DLog(params object[] args) => Log(LogType.Debug, args);
    /// <summary>
    /// Logs an warning message
    /// </summary>
    /// <param name="args">Any arguments for the log message</param>
    public void WLog(params object[] args) => Log(LogType.Warning, args);
    /// <summary>
    /// Logs an error message
    /// </summary>
    /// <param name="args">Any arguments for the log message</param>
    public void ELog(params object[] args) => Log(LogType.Error, args);

    /// <summary>
    /// Gets a tail of the log
    /// NOTE: NOT IMPLEMENTED
    /// </summary>
    /// <param name="length">The number of lines to fetch</param>
    /// <returns>NOT IMPLEMENTED</returns>
    public string GetTail(int length = 50) => "Not implemented";

    static FileFlows.Plugin.ILogger _Instance;
    
    /// <summary>
    /// Gets the instance of the ILogger being used
    /// </summary>
    public static FileFlows.Plugin.ILogger Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = new Logger();
            return _Instance;
        }
        set { _Instance = value; }
    }
}
