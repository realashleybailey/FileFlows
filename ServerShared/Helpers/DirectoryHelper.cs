namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// A helper class to manage the directories used by FileFlows Server and Node
/// </summary>
public class DirectoryHelper
{
    /// <summary>
    /// Gets if this is a Docker instance or not
    /// </summary>
    public static bool IsDocker { get; private set; }
    /// <summary>
    /// Gets if this is a Node or Server
    /// </summary>
    public static bool IsNode { get; private set; }

    public static void Init(bool isDocker, bool isNode)
    {
        DirectoryHelper.IsDocker = isDocker;
        DirectoryHelper.IsNode = isNode;
        
        InitLoggingDirectory();
        InitDataDirectory();

        FlowRunnerDirectory = Path.Combine(BaseDirectory, "FlowRunner");
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
                var dllDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(dllDir))
                    throw new Exception("Failed to find DLL directory");
                _BaseDirectory = new DirectoryInfo(dllDir).Parent.FullName;
            }
            return _BaseDirectory;
        }
    }
    
    private static string ExecutingDirectory => Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);


    /// <summary>
    /// Inits the logging directory and moves any files if they need to be moved
    /// </summary>
    private static void InitLoggingDirectory()
    {
        string dir = Path.Combine(BaseDirectory, "Logs");
        if (Directory.Exists(dir) == false)
            Directory.CreateDirectory(dir);
        
        // look for logs from other directories
        string localLogs = Path.Combine(ExecutingDirectory, "Logs");
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
        
        const string nodeConfig = "node.config";
        NodeConfigFile = Path.Combine(dir, nodeConfig);
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "fileflows.config")))
            File.Move(Path.Combine(Directory.GetCurrentDirectory(), "fileflows.config"), NodeConfigFile);
        

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
    /// Gets the flow runner directory
    /// </summary>
    public static string FlowRunnerDirectory { get; private set; }

    /// <summary>
    /// Gets the logging directory
    /// </summary>
    public static string PluginsDirectory
    {
        get
        {
            // docker we expose this in the data directory so we
            // reduce how many things we have to map out
            if (IsDocker) 
                return Path.Combine(DataDirectory, "Plugins");
            return  Path.Combine(BaseDirectory, IsNode ? "Node" : "Server", "Plugins");
        }
    }
    /// <summary>
    /// Gets the location of the encryption key file
    /// </summary>
    public static string EncryptionKeyFile { get;private set; }

    /// <summary>
    /// Gets the location of the node configuration file
    /// </summary>
    public static string NodeConfigFile { get; private set; }


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
                file.MoveTo(Path.Combine(destination, file.Name));
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