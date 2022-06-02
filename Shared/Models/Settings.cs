namespace FileFlows.Shared.Models;

/// <summary>
/// Settings for FileFlows
/// </summary>
public class Settings : FileFlowObject
{
    /// <summary>
    /// Gets or sets if plugins should automatically be updated when new version are available online
    /// </summary>
    public bool AutoUpdatePlugins { get; set; }

    /// <summary>
    /// Gets or sets if the server should automatically update when a new version is available online
    /// </summary>
    public bool AutoUpdate { get; set; }

    /// <summary>
    /// Gets or sets if nodes should be automatically updated when the server is updated
    /// </summary>
    public bool AutoUpdateNodes { get; set; }

    /// <summary>
    /// Gets or sets if telemetry should be disabled
    /// </summary>
    public bool DisableTelemetry { get; set; }
    
    private List<string> _PluginRepositoryUrls = new ();
    /// <summary>
    /// Gets or sets a list of available URLs to additional plugin repositories
    /// </summary>
    public List<string> PluginRepositoryUrls
    {
        get => _PluginRepositoryUrls;
        set => _PluginRepositoryUrls = value ?? new();
    }

    /// <summary>
    /// Gets or sets if this is running on Windows
    /// </summary>
    public bool IsWindows { get; set; }
    
    /// <summary>
    /// Gets or sets if this is running inside Docker
    /// </summary>
    public bool IsDocker { get; set; }
    
    /// <summary>
    /// Gets or sets the FileFlows version number
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Gets or sets the type of database to use
    /// </summary>
    public DatabaseType DbType { get; set; }
    
    /// <summary>
    /// Gets or sets the db server to use
    /// </summary>
    public string DbServer { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the database
    /// </summary>
    public string DbName { get; set; }
    
    /// <summary>
    /// Gets or sets the user used to connect to the database
    /// </summary>
    public string DbUser { get; set; }
    
    /// <summary>
    /// Gets or sets the password used to connect to the database
    /// </summary>
    public string DbPassword { get; set; }

    /// <summary>
    /// Gets or sets if the user can change database settings
    /// </summary>
    public bool DbAllowed { get; set; }
}

/// <summary>
/// The types of Databases supported
/// </summary>
public enum DatabaseType
{
    /// <summary>
    /// SQLite Database
    /// </summary>
    Sqlite = 0,
    /// <summary>
    /// Microsoft SQL Server
    /// </summary>
    SqlServer = 1,
    /// <summary>
    /// MySql / MariaDB
    /// </summary>
    MySql = 2
}