namespace FileFlows.ServerShared;

public class FileLogger : Plugin.ILogger
{
    private string logFile;

    private string LogPrefix;
    private string LoggingPath;

    private DateOnly LogDate = DateOnly.MinValue;

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

        string message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffff") + " [" + prefix + "]-> "  + string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive ? x.ToString() :
            x is string ? x.ToString() :
            System.Text.Json.JsonSerializer.Serialize(x)));
        Console.WriteLine(message);
        System.IO.File.AppendAllText(logFile, message + Environment.NewLine);
    }

    public void ILog(params object[] args) => Log(LogType.Info, args);
    public void DLog(params object[] args) => Log(LogType.Debug, args);
    public void WLog(params object[] args) => Log(LogType.Warning, args);
    public void ELog(params object[] args) => Log(LogType.Error, args);

    static FileFlows.Plugin.ILogger _Instance;
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

    public string GetTail(int length = 50) => "Not implemented";
}
