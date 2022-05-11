namespace FileFlows.Server;

public class Logger : FileFlows.Plugin.ILogger
{
    Queue<LogTailItem> LogTailDebug = new (300);
    Queue<LogTailItem> LogTailInfo = new (300);
    Queue<LogTailItem> LogTailWarning = new (100);
    Queue<LogTailItem> LogTailError = new (100);

    public string LogFile { get; private set; }

    public Logger()
    {
        this.LogFile = Path.Combine(DirectoryHelper.LoggingDirectory, DirectoryHelper.IsNode ? "FileFlowsNode.log" : "FileFlows.log");
        if (File.Exists(LogFile))
        {
            try
            {
                File.Move(LogFile, LogFile + ".old", true);
            }
            catch (Exception) { }

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

        return now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " - " + prefix + " -> ";
    }

    private Mutex mutex = new Mutex();

    internal enum LogType { Error, Warning, Info, Debug }
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
            var tail = type switch
            {
                LogType.Error => LogTailError,
                LogType.Warning => LogTailWarning,
                LogType.Info => LogTailInfo,
                _ => LogTailDebug,
            };
            tail.Enqueue(new LogTailItem
            {
                Date = DateTime.Now,
                Type = (byte)type,
                Message = System.Text.Encoding.UTF8.GetBytes(message)
            });
            if (Program.WindowsGui == true)
                return; // windows gui already captures all this

            var fi = new FileInfo(LogFile);
            if(fi.Exists && fi.Length > 10_000_000)
            {
                // larger than 10MB, move it and create new one
                try
                {
                    fi.MoveTo(LogFile + ".old", true);
                }
                catch (Exception)
                {
                    // silent fail here
                }
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
        LogTailItem[] filtered;
        try
        {
            List<LogTailItem> tails = new();
            tails.AddRange(LogTailError);
            if((int)logLevel >= (int)Plugin.LogType.Warning)
                tails.AddRange(LogTailWarning);
            if((int)logLevel >= (int)Plugin.LogType.Info)
                tails.AddRange(LogTailInfo);
            if((int)logLevel >= (int)Plugin.LogType.Debug)
                tails.AddRange(LogTailDebug);

            filtered = tails.OrderBy(x => x.Date).ToArray();
        }
        finally
        {
            mutex.ReleaseMutex();
        }

        bool all = filtered.Length <= length;
        return string.Join(Environment.NewLine, 
            (all ? filtered : filtered.Skip(filtered.Count() - length))
            .Select(x => System.Text.Encoding.UTF8.GetString(x.Message)));
    }

    static FileFlows.Plugin.ILogger _Instance;
    public static FileFlows.Plugin.ILogger Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = new Logger();
            return _Instance;
        }
        set
        {
            if (value != null)
                _Instance = value;
        }
    }
    
    private struct LogTailItem
    {
        public DateTime Date { get; set; }
        public byte Type { get; set; }
        public byte[] Message { get; set; }
    }
}
