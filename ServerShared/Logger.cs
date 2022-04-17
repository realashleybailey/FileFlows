namespace FileFlows.ServerShared;

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

    public void ILog(params object[] args) => Log(LogType.Info, args);
    public void DLog(params object[] args) => Log(LogType.Debug, args);
    public void WLog(params object[] args) => Log(LogType.Warning, args);
    public void ELog(params object[] args) => Log(LogType.Error, args);

    public string GetTail(int length = 50) => "Not implemented";

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
}
