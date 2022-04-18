using FileFlows.Shared.Models;

namespace FileFlows.Server;

public class Logger : FileFlows.Plugin.ILogger
{
    Queue<string> LogTail = new Queue<string>(300);

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
            File.AppendAllText(LogFile, message + Environment.NewLine);
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

    public string GetTail(int length = 50)
    {
        if(length == -1)
            return string.Join(Environment.NewLine, LogTail.ToArray());
        if (LogTail.Count() <= length)
            return String.Join(Environment.NewLine, LogTail);
        return String.Join(Environment.NewLine, LogTail.Skip(LogTail.Count() - length));
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
