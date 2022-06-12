namespace FileFlows.Server.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using FileFlows.Server.Workers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Database.Managers;

/// <summary>
/// Settings Controller
/// </summary>
[Route("/api/settings")]
public class SettingsController : Controller
{
    private static Settings Instance;
    private static SemaphoreSlim semaphore = new SemaphoreSlim(1);

    /// <summary>
    /// Whether or not the system is configured
    /// </summary>
    /// <returns>return 2 if everything is configured, 1 if partially configured, 0 if not configured</returns>
    [HttpGet("is-configured")]
    public async Task<int> IsConfigured()
    {
        // this updates the TZ with the TZ from the client if not set
        var settings = await Get();

        var libs = new LibraryController().GetData().Result?.Any() == true;
        var flows = new FlowController().GetData().Result?.Any() == true;
        if (libs && flows)
            return 2;
        if (flows)
            return 1;
        return 0;
    }

    /// <summary>
    /// Checks latest version from fileflows.com
    /// </summary>
    /// <returns>The latest version number if greater than current</returns>
    [HttpGet("check-update-available")]
    public async Task<string> CheckLatestVersion()
    {
        var settings = await new SettingsController().Get();
        if (settings.DisableTelemetry != false)
            return string.Empty; 
        try
        {
            var result = Workers.ServerUpdater.GetLatestOnlineVersion();
            if (result.updateAvailable == false)
                return string.Empty;
            return result.onlineVersion.ToString();
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed checking latest version: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return String.Empty;
        }
    }

    /// <summary>
    /// Get the system settings
    /// </summary>
    /// <returns>The system settings</returns>
    [HttpGet("ui-settings")]
    public async Task<SettingsUiModel> GetUiModel()
    {
        var settings = await Get();
        var license = License.FromCode(AppSettings.Instance.LicenseCode);
        if (license.Status == LicenseStatus.Unlicensed && string.IsNullOrWhiteSpace(AppSettings.Instance.LicenseKey) == false)
            license.Status = LicenseStatus.Invalid;
        // clone it so we can remove some properties we dont want passed to the UI
        string json = JsonSerializer.Serialize(settings);
        var uiModel = JsonSerializer.Deserialize<SettingsUiModel>(json);
        await SetLicenseFields(uiModel, license);
        


        string dbConnStr = AppSettings.Instance.DatabaseMigrateConnection?.EmptyAsNull() ?? AppSettings.Instance.DatabaseConnection;
        if (string.IsNullOrWhiteSpace(dbConnStr) || dbConnStr.ToLower().Contains("sqlite"))
            uiModel.DbType = DatabaseType.Sqlite;
        else if (dbConnStr.Contains(";Uid="))
            new MySqlDbManager(string.Empty).PopulateSettings(uiModel, dbConnStr);
        else
            new SqlServerDbManager(string.Empty).PopulateSettings(uiModel, dbConnStr);
        
        return uiModel;
    }

    /// <summary>
    /// Get the system settings
    /// </summary>
    /// <returns>The system settings</returns>
    [HttpGet]
    public async Task<Settings> Get()
    {
        await semaphore.WaitAsync();
        try
        {
            if (Instance == null)
            {
                Instance = await DbHelper.Single<Settings>();
            }
            Instance.IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            Instance.IsDocker = Program.Docker;

            if (LicenseHelper.IsLicensed() == false)
            {
                Instance.LogFileRetention = 2;
            }
            
            return Instance;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task SetLicenseFields(SettingsUiModel settings, License license)
    {
        settings.LicenseKey = AppSettings.Instance.LicenseKey;
        settings.LicenseEmail  = AppSettings.Instance.LicenseEmail;
        settings.LicenseFlags = license.Flags.ToString();
        settings.LicenseProcessingNodes = LicenseHelper.GetLicensedProcessingNodes();
        settings.LicenseExpiryDate = license.ExpirationDateUtc.ToLocalTime();
        settings.LicenseStatus = license.Status.ToString();
    }

    /// <summary>
    /// Save the system settings
    /// </summary>
    /// <param name="model">the system settings to save</param>
    /// <returns>The saved system settings</returns>
    [HttpPut("ui-settings")]
    public async Task SaveUiModel([FromBody] SettingsUiModel model)
    {
        if (model == null)
            return;

        Save(new ()
        {
            IsPaused = model.IsPaused,
            LogFileRetention = model.LogFileRetention,
            LogDatabaseRetention = model.LogDatabaseRetention,
            AutoUpdate = model.AutoUpdate,
            DisableTelemetry = model.DisableTelemetry,
            AutoUpdateNodes = model.AutoUpdateNodes,
            AutoUpdatePlugins = model.AutoUpdatePlugins,
            LogQueueMessages = model.LogQueueMessages,
            PluginRepositoryUrls = model.PluginRepositoryUrls
        });
        
        // validate license it
        var licResult = await LicenseValidatorWorker.ValidateLicense(model.LicenseEmail, model.LicenseKey);
        AppSettings.Instance.LicenseKey = model.LicenseKey;
        AppSettings.Instance.LicenseEmail = model.LicenseEmail;
        AppSettings.Instance.LicenseCode = licResult.LicenseCode;

        var newConnectionString = GetConnectionString(model, model.DbType);
        if (IsConnectionSame(AppSettings.Instance.DatabaseConnection, newConnectionString) == false)
        {
            // need to migrate the database
            AppSettings.Instance.DatabaseMigrateConnection = newConnectionString?.EmptyAsNull() ?? DbManager.GetDefaultConnectionString();
        }
        // save AppSettings with updated license and db migration if set
        AppSettings.Instance.Save();
    }
    /// <summary>
    /// Save the system settings
    /// </summary>
    /// <param name="model">the system settings to save</param>
    /// <returns>The saved system settings</returns>
    internal async Task Save(Settings model)
    {
        if (model == null)
            return;
        var settings = await Get() ?? model;
        model.Name = settings.Name;
        model.Uid = settings.Uid;
        model.Version = Globals.Version;
        model.DateCreated = settings.DateCreated;
        model.IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        model.IsDocker = Program.Docker;
        Instance = model;
        await DbHelper.Update(model);
    }

    private bool IsConnectionSame(string original, string newConnection)
    {
        if (IsSqliteConnection(original) && IsSqliteConnection(newConnection))
            return true;
        return original == newConnection;
    }

    private bool IsSqliteConnection(string connString)
    {
        if (string.IsNullOrWhiteSpace(connString))
            return true;
        return connString.IndexOf("FileFlows.sqlite") > 0;
    }

    private string GetConnectionString(SettingsUiModel settings, DatabaseType dbType)
    {
        if (dbType == DatabaseType.SqlServer)
            return new SqlServerDbManager(string.Empty).GetConnectionString(settings.DbServer, settings.DbName, settings.DbUser,
                settings.DbPassword);
        if (dbType == DatabaseType.MySql)
            return new MySqlDbManager(string.Empty).GetConnectionString(settings.DbServer, settings.DbName, settings.DbUser,
                settings.DbPassword);
        return string.Empty;
    }
    

    /// <summary>
    /// Tests a database connection
    /// </summary>
    /// <param name="model">The database connection info</param>
    /// <returns>OK if successful, otherwise a failure message</returns>
    [HttpPost("test-db-connection")]
    public string TestDbConnection([FromBody] DbConnectionInfo model)
    {
        if (model == null)
            throw new ArgumentException(nameof(model));

        if (model.Type == DatabaseType.SqlServer)
            return new SqlServerDbManager(string.Empty).Test(model.Server, model.Name, model.User, model.Password)
                ?.EmptyAsNull() ?? "OK";
        if (model.Type == DatabaseType.MySql)
            return new MySqlDbManager(string.Empty).Test(model.Server, model.Name, model.User, model.Password)
                ?.EmptyAsNull() ?? "OK";
        
        return "Unsupported database type";
    }

    /// <summary>
    /// Triggers a check for an update
    /// </summary>
    [HttpPost("check-for-update-now")]
    public async Task CheckForUpdateNow()
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return;

        if (ServerUpdater.Instance == null)
            return;

        _ = Task.Run(async () =>
        {
            await Task.Delay(1);
            return ServerUpdater.Instance.RunCheck();
        });
    }
    
}

/// <summary>
/// Database connection details
/// </summary>
public class DbConnectionInfo
{
    /// <summary>
    /// Gets or sets the server address
    /// </summary>
    public string Server { get; set; }
    /// <summary>
    /// Gets or sets the database name
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the connecting user
    /// </summary>
    public string User { get; set; }
    /// <summary>
    /// Gets or sets the password used
    /// </summary>
    public string Password { get; set; }
    /// <summary>
    /// Gets or sets the database type
    /// </summary>
    public DatabaseType Type { get; set; }
}
