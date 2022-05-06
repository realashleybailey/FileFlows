namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// A helper class to manage the directories used by FileFlows Server and Node
/// </summary>
public class DirectoryHelper
{
    private static bool IsDocker { get; set; }
    private static bool IsNode { get; set; }

    public static void Init(bool isDocker, bool isNode)
    {
        DirectoryHelper.IsDocker = isDocker;
        DirectoryHelper.IsNode = isNode;
        
        InitLoggingDirectory();
        if(isNode == false)
            InitDataDirectory();
    }

    private static string _BaseDirectory;

    /// <summary>
    /// Gets the base directory of FileFlows
    /// eg %appdata%\FileFlows
    /// </summary>
    public static string BaseDirectory
    {
        get
        {
            if (string.IsNullOrEmpty(_BaseDirectory))
            {
                if(IsDocker)
                    _BaseDirectory = Directory.GetCurrentDirectory();
                else
                {
                    string dir = Directory.GetCurrentDirectory();
                    if (dir.Replace("\\", "/").ToLower().EndsWith("/server") ||
                        dir.Replace("\\", "/").ToLower().EndsWith("/node"))
                        dir = new DirectoryInfo(dir).Parent.FullName;
                    _BaseDirectory = dir;
                }
            }
            return _BaseDirectory;
        }
    }

    /// <summary>
    /// Inits the logging directory and moves any files if they need to be moved
    /// </summary>
    private static void InitLoggingDirectory()
    {
        string dir = IsDocker ? "/app/Logs" : Path.Combine(BaseDirectory, "Logs");
        if (Directory.Exists(dir) == false)
            Directory.CreateDirectory(dir);
        
        // look for logs from other directories
        string localLogs = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        if(localLogs != dir && Directory.Exists(localLogs))
            MoveDirectoryContent(localLogs, dir);
        
        LoggingDirectory = dir;
    }

    /// <summary>
    /// Inits the data directory and moves any files if they need to be moved
    /// </summary>
    private static void InitDataDirectory()
    {
        string dir = Path.Combine(BaseDirectory, "Data");
        if (Directory.Exists(dir) == false)
            Directory.CreateDirectory(dir);
        DataDirectory = dir;
        
        // look for logs from other directories
        string localData = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        if(localData != dir && Directory.Exists(localData))
            MoveDirectoryContent(localData, dir);

        const string encryptKey = "encryptionkey.txt";
        EncryptionKeyFile = Path.Combine(dir, encryptKey);
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), encryptKey)))
            File.Move(Path.Combine(Directory.GetCurrentDirectory(), encryptKey), EncryptionKeyFile);
        
        DatabaseDirectory = IsDocker == false ? dir : Path.Combine(dir, "Data");
        if (Directory.Exists(DatabaseDirectory) == false)
            Directory.CreateDirectory(DatabaseDirectory);
        
    }

    /// <summary>
    /// Gets the logging directory
    /// </summary>
    public static string LoggingDirectory { get; private set; }

    /// <summary>
    /// Gets the data directory
    /// </summary>
    public static string DataDirectory { get; private set; }
    
    /// <summary>
    /// Gets the directory the database is saved in
    /// </summary>
    public static string DatabaseDirectory { get; private set; }

    /// <summary>
    /// Gets the logging directory
    /// </summary>
    public static string PluginsDirectory => Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
    
    /// <summary>
    /// Gets the location of hte encryption key file
    /// </summary>
    public static string EncryptionKeyFile { get;private set; }




    private static void MoveDirectoryContent(string source, string destination)
    {
        if(Directory.Exists(destination) == false)
            Directory.CreateDirectory(destination);

        if(Directory.Exists(source) == false)
            return;

        var dirInfo = new DirectoryInfo(source);
        foreach(var dir in dirInfo.GetDirectories())
        {
            MoveDirectoryContent(dir.FullName, Path.Combine(destination, dir.Name));
        }

        foreach(var file in dirInfo.GetFiles())
        {
            try
            {
                file.MoveTo(destination);
            }
            catch(Exception) { }
        }

        try
        {
           dirInfo.Delete(true); 
        }
        catch(Exception) { }
    }
}