namespace FileFlows.Shared.Models;

/// <summary>
/// Model for the settings in the UI, differs from the actual settings model as this some combined settings
/// </summary>
public class SettingsUiModel:Settings
{

    /// <summary>
    /// Gets or sets the license email
    /// </summary>
    public string LicenseEmail { get; set; }

    /// <summary>
    /// Gets or sets the licensed key
    /// </summary>
    public string LicenseKey { get; set; }

    /// <summary>
    /// Gets the license flags for the user
    /// </summary>
    public string LicenseFlags { get; set; }
    /// <summary>
    /// Gets the license expiry date for the user
    /// </summary>
    public DateTime LicenseExpiryDate { get; set; }
    /// <summary>
    /// Gets the licensed processing nodes for the user
    /// </summary>
    public int LicenseProcessingNodes { get; set; }
    /// <summary>
    /// Gets the licensed status for the user
    /// </summary>
    public string LicenseStatus { get; set; }

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
    
}