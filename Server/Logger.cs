using FileFlows.ServerShared;

namespace FileFlows.Server;

public class Logger
{
    static FileFlows.Plugin.ILogger _Instance;
    public static FileFlows.Plugin.ILogger Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = new FileLogger(DirectoryHelper.LoggingDirectory, "FileFlows");
            return _Instance;
        }
        set
        {
            if (value != null)
                _Instance = value;
        }
    }
}
