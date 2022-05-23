namespace FileFlows.Server;

public class Logger : FileFlows.Plugin.ILogger
{
    //Queue<LogTailItem> LogTailDebug = new (300);
    //Queue<LogTailItem> LogTailInfo = new (300);
    //Queue<LogTailItem> LogTailWarning = new (100);
    //Queue<LogTailItem> LogTailError = new (100);

    public string LogFile { get; private set; }

    public Logger()
    {
        string prefix = DirectoryHelper.IsNode ? "FileFlowsNode" : "FileFlows";
        this.LogFile = Path.Combine(DirectoryHelper.LoggingDirectory, prefix + ".log");
        if (File.Exists(LogFile))
        {
            try
            {
                var fi = new FileInfo(LogFile);
                var dest = Path.Combine(DirectoryHelper.LoggingDirectory, $"{prefix}_{fi.CreationTime.ToString("yyyy-MM-dd hh-mm-ss")}.log");
                File.Move(LogFile, dest, true);
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
            //var tail = type switch
            //{
            //    LogType.Error => LogTailError,
            //    LogType.Warning => LogTailWarning,
            //    LogType.Info => LogTailInfo,
            //    _ => LogTailDebug,
            //};
            //tail.Enqueue(new LogTailItem
            //{
            //    Date = DateTime.Now,
            //    Type = (byte)type,
            //    Message = System.Text.Encoding.UTF8.GetBytes(message)
            //});

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
        if (length <= 0 || length > 1000)
            length = 1000;

        mutex.WaitOne();
        //LogTailItem[] filtered;
        try
        {
            return GetTailActual(length, logLevel);
            //List<LogTailItem> tails = new();
            //tails.AddRange(LogTailError);
            //if((int)logLevel >= (int)Plugin.LogType.Warning)
            //    tails.AddRange(LogTailWarning);
            //if((int)logLevel >= (int)Plugin.LogType.Info)
            //    tails.AddRange(LogTailInfo);
            //if((int)logLevel >= (int)Plugin.LogType.Debug)
            //    tails.AddRange(LogTailDebug);
//
            //filtered = tails.OrderBy(x => x.Date).ToArray();
        }
        finally
        {
            mutex.ReleaseMutex();
        }

        //bool all = filtered.Length <= length;
        //return string.Join(Environment.NewLine, 
        //    (all ? filtered : filtered.Skip(filtered.Count() - length))
        //    .Select(x => System.Text.Encoding.UTF8.GetString(x.Message)));
    }

    private string GetTailActual(int length, Plugin.LogType logLevel)
    {
        StreamReader reader = new StreamReader(LogFile);
        reader.BaseStream.Seek(0, SeekOrigin.End);
        int count = 0;
        while ((count < length) && (reader.BaseStream.Position > 0))
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
        }).ToArray();
        reader.Close();
        return string.Join("\n", arr);
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
