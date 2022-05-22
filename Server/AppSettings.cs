using System.Text.Json.Serialization;

namespace FileFlows.Server;

/// <summary>
/// Application settings that are saved to the appsettings.json file 
/// </summary>
internal class AppSettings
{
    /// <summary>
    /// Gets or sets the database connection to use
    /// </summary>
    public string DatabaseConnection { get; set; }

    /// <summary>
    /// Gets or sets the encryption key to use
    /// </summary>
    public string EncryptionKey { get; set; }






    private static AppSettings? _Instance;

    /// <summary>
    /// The AppSettings instance
    /// </summary>
    public static AppSettings Instance
    {
        get
        {
            _Instance ??= Load();
            return _Instance;
        }
    }

    /// <summary>
    /// Saves the app settings
    /// </summary>
    public void Save() 
    {
        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition  = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull
        };
        
        string json = JsonSerializer.Serialize(this, serializerOptions);
        File.WriteAllText(DirectoryHelper.ServerConfigFile, json);
    }
    
    /// <summary>
    /// Loads the app settings
    /// </summary>
    /// <returns>the app settings</returns>
    public static AppSettings Load()
    {
        string file = DirectoryHelper.ServerConfigFile;
        if (File.Exists(file) == false)
        {
            AppSettings settings = new();
            if (File.Exists(DirectoryHelper.EncryptionKeyFile))
            {
                settings.EncryptionKey = File.ReadAllText(DirectoryHelper.EncryptionKeyFile);
                File.Delete(DirectoryHelper.EncryptionKeyFile);
            }

            if (string.IsNullOrWhiteSpace(settings.EncryptionKey))
                settings.EncryptionKey = Guid.NewGuid().ToString();
            
            settings.Save();
            
            return settings;
        }
        
        try
        {
            string json = File.ReadAllText(file);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? new ();
        }
        catch (Exception) { }
        return new();
    }
}