using FileFlows.Shared.Models;

namespace FileFlows.Server;

public class Logger : FileFlows.Plugin.ILogger
{
    Queue<string> LogTail = new Queue<string>(3000);

    private string _LoggingPath;
    public string LoggingPath
    {
        get => _LoggingPath;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                bool windows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
                _LoggingPath = windows ? Path.Combine(Program.GetAppDirectory(), "Logs") : "/app/Logs";
            }
            else 
            {
                _LoggingPath = value;
            }
            LogFile = Path.Combine(_LoggingPath, "FileFlows.log");
        }
    }

    public string LogFile { get; private set; }

    public Logger(string loggingPath)
    {
        this.LoggingPath = loggingPath;
        if (File.Exists(LogFile))
        {
            File.Move(LogFile, LogFile + ".old", true);
        }
    }
    
    internal static string GetPrefix(LogType type)
    {
        string prefix = type switch
        {
            LogType.Info => "INFO",
            LogType.Error => "ERRR",
            LogType.Warning => "WARN",
            LogType.Debug => "DBUG",
            _ => ""
        };

        var now = DateTime.Now;

        return now.ToString("yyyy-MM-dd HH:mm:ss.ffff") + " - " + prefix + " -> ";
    }

    private Mutex mutex = new Mutex();

    internal enum LogType { Error, Warning, Debug, Info }
    private void Log(LogType type, object[] args)
    {
        string prefix = GetPrefix(type);

        string message = prefix + string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive ? x.ToString() :
            x is string ? x.ToString() :
            System.Text.Json.JsonSerializer.Serialize(x)));

        mutex.WaitOne();
        try
        {
            Console.WriteLine(message);
            LogTail.Enqueue(message);
            var fi = new FileInfo(LogFile);
            if(fi.Exists && fi.Length > 10_000)
            {
                // larger than 10MB, move it and create new one
                fi.MoveTo(LogFile + ".old", true);
            }
            File.AppendAllText(LogFile, message + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in Logger: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    public void ILog(params object[] args) => Log(LogType.Info, args);
    public void DLog(params object[] args) => Log(LogType.Debug, args);
    public void WLog(params object[] args) => Log(LogType.Warning, args);
    public void ELog(params object[] args) => Log(LogType.Error, args);


    public string GetTail(int length = 50) => GetTail(length, Plugin.LogType.Info);

    internal string GetTail(int length = 50, Plugin.LogType logLevel = Plugin.LogType.Info)
    {
        if (length <= 0 || length > 300)
            length = 300;

        mutex.WaitOne();
        string[] filtered;
        try
        {
            filtered = logLevel == Plugin.LogType.Debug ? LogTail.ToArray() : LogTail.Where(x =>
            {
                // if log level is error, must contain ERRR
                if (logLevel == Plugin.LogType.Error)
                    return x.Contains("ERRR");
                // else if log level below error, if it contains ERR, the logging level also includes this
                if (x.Contains("ERRR"))
                    return x.Contains("ERRR");

                // same logic as error, warning is next highest
                if (logLevel == Plugin.LogType.Warning)
                    return x.Contains("WARN");
                if (x.Contains("WARN"))
                    return false;

                if (logLevel == Plugin.LogType.Info)
                    return x.Contains("INFO");
                if (x.Contains("INFO"))
                    return false;

                return true;
            }).ToArray();
        }
        finally
        {
            mutex.ReleaseMutex();
        }
        if (length == -1)
            return string.Join(Environment.NewLine, filtered.ToArray());
        if (filtered.Count() <= length)
            return String.Join(Environment.NewLine, filtered);
        return String.Join(Environment.NewLine, filtered.Skip(filtered.Count() - length));
    }

    static FileFlows.Plugin.ILogger _Instance;
    public static FileFlows.Plugin.ILogger Instance
    {
        get
        {
            if (_Instance == null)
            {
                // we pass in null here since this logger is created before we have access to the settings....
                // may have to alter this later
                _Instance = new Logger(null);
            }
            return _Instance;
        }
        set
        {
            if (value != null)
                _Instance = value;
        }
    }
}
