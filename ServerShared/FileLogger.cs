namespace FileFlows.ServerShared;

/// <summary>
/// A Logger that writes its output to file
/// </summary>
public class FileLogger : Plugin.ILogger
{
    private string logFile;

    private string LogPrefix;
    private string LoggingPath;

    private DateOnly LogDate = DateOnly.MinValue;

    /// <summary>
    /// Creates a file logger
    /// </summary>
    /// <param name="loggingPath">The path where to save the log file to</param>
    /// <param name="logPrefix">The prefix to use for the log file name</param>
    public FileLogger(string loggingPath, string logPrefix)
    {
        this.LoggingPath = loggingPath;
        this.LogPrefix = logPrefix;
    }

    private enum LogType { Error, Warning, Debug, Info }
    private void Log(LogType type, object[] args)
    {
        if(DateOnly.FromDateTime(DateTime.Now) != LogDate)
        {
            // need a new log file
            SetLogFile();
        }

        string prefix = type switch
        {
            LogType.Info => "INFO",
            LogType.Error => "ERRR",
            LogType.Warning => "WARN",
            LogType.Debug => "DBUG",
            _ => ""
        };

        string message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + prefix + "]-> "  + string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive ? x.ToString() :
            x is string ? x.ToString() :
            System.Text.Json.JsonSerializer.Serialize(x)));
        Console.WriteLine(message);
        System.IO.File.AppendAllText(logFile, message + Environment.NewLine);
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

    static FileFlows.Plugin.ILogger _Instance;
    
    /// <summary>
    /// Gets an isntance of the ILogger being used
    /// </summary>
    public static FileFlows.Plugin.ILogger Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = new Logger();
            return _Instance;
        }
    }

    private void SetLogFile()
    {
        this.LogDate = DateOnly.FromDateTime(DateTime.Now);
        this.logFile = Path.Combine(LoggingPath, LogPrefix + "-" + LogDate.ToDateTime(new TimeOnly()).ToString("MMMdd") + ".log");
    }

    
    /// <summary>
    /// Gets a tail of the log
    /// NOTE: NOT IMPLEMENTED
    /// </summary>
    /// <param name="length">The number of lines to fetch</param>
    /// <returns>NOT IMPLEMENTED</returns>
    public string GetTail(int length = 50) => "Not implemented";
}
