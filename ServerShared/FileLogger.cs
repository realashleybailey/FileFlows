namespace FileFlows.ServerShared;

/// <summary>
/// A Logger that writes its output to file
/// </summary>
public class FileLogger : Plugin.ILogger
{
    private string LogPrefix;
    private string LoggingPath;

    private DateOnly LogDate = DateOnly.MinValue;

    private SemaphoreSlim mutex = new SemaphoreSlim(1);

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
        mutex.Wait();
        try
        {
            string logFile = GetLogFilename();
            string prefix = type switch
            {
                LogType.Info => "INFO",
                LogType.Error => "ERRR",
                LogType.Warning => "WARN",
                LogType.Debug => "DBUG",
                _ => ""
            };

            string message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + prefix + "]-> " + string.Join(
                ", ", args.Select(x =>
                    x == null ? "null" :
                    x.GetType().IsPrimitive ? x.ToString() :
                    x is string ? x.ToString() :
                    System.Text.Json.JsonSerializer.Serialize(x)));
            Console.WriteLine(message);
            File.AppendAllText(logFile, message + Environment.NewLine);
        }
        finally
        {
            mutex.Release();
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
    
    /// <summary>
    /// Gets a tail of the log
    /// </summary>
    /// <param name="length">The number of lines to fetch</param>
    /// <returns>a tail of the log</returns>
    public string GetTail(int length = 50) => GetTail(length, Plugin.LogType.Info);

    /// <summary>
    /// Gets a tail of the log
    /// </summary>
    /// <param name="length">The number of lines to fetch</param>
    /// <param name="logLevel">the log level</param>
    /// <returns>a tail of the log</returns>
    public string GetTail(int length = 50, Plugin.LogType logLevel = Plugin.LogType.Info)
    {
        if (length <= 0 || length > 1000)
            length = 1000;

        mutex.Wait();
        try
        {
            return GetTailActual(length, logLevel);
        }
        finally
        {
            mutex.Release();
        }
    }

    private string GetTailActual(int length, Plugin.LogType logLevel)
    {
        string logFile = GetLogFilename();
        if (string.IsNullOrEmpty(logFile) || File.Exists(logFile) == false)
            return string.Empty;
        StreamReader reader = new StreamReader(logFile);
        reader.BaseStream.Seek(0, SeekOrigin.End);
        int count = 0;
        int max = length;
        if (logLevel != Plugin.LogType.Debug)
            max = 5000;
        while ((count < max) && (reader.BaseStream.Position > 0))
        {
            reader.BaseStream.Position--;
            int c = reader.BaseStream.ReadByte();
            if (reader.BaseStream.Position > 0)
                reader.BaseStream.Position--;
            if (c == Convert.ToInt32('\n'))
            {
                ++count;
            }
        }

        string str = reader.ReadToEnd();
        if (logLevel == Plugin.LogType.Debug)
            return str;
        
        string[] arr = str.Replace("\r", "").Split('\n');
        arr = arr.Where(x =>
        {
            if (logLevel < Plugin.LogType.Debug && x.Contains("DBUG"))
                return false;
            if (logLevel < Plugin.LogType.Info && x.Contains("INFO"))
                return false;
            if (logLevel < Plugin.LogType.Warning && x.Contains("WARN"))
                return false;
            return true;
        }).Take(length).ToArray();
        reader.Close();
        return string.Join("\n", arr);
    }

    
    /// <summary>
    /// Gets the name of the filename to log to
    /// </summary>
    /// <returns>the name of the filename to log to</returns>
    public string GetLogFilename()
    {
        string file = Path.Combine(LoggingPath, LogPrefix + "-" + DateTime.Now.ToString("MMMdd"));
        for (int i = 1; i < 100; i++)
        {
            FileInfo fi = new(file + "-" + i.ToString("D2"));
            if (fi.Exists == false)
                return fi.FullName;
            if (fi.Length < 10_000_000)
                return fi.FullName;
        }

        return string.Empty;
    }
}
