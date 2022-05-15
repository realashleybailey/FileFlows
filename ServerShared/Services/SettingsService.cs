namespace FileFlows.ServerShared.Services;

using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

/// <summary>
/// Interface for the Settings service which allows accessing of all the system settings
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the system settings
    /// </summary>
    /// <returns>the system settings</returns>
    Task<Settings> Get();
}

/// <summary>
/// An instance of the Settings Service which allows accessing of the system settings
/// </summary>
public class SettingsService : Service, ISettingsService
{

    /// <summary>
    /// A loader to load an instance of the Settings
    /// </summary>
    public static Func<ISettingsService> Loader { get; set; }

    /// <summary>
    /// Loads an instance of the settings service
    /// </summary>
    /// <returns>an instance of the settings service</returns>
    public static ISettingsService Load()
    {
        if (Loader == null)
            return new SettingsService();
        return Loader.Invoke();
    }

    /// <summary>
    /// Gets the system settings
    /// </summary>
    /// <returns>the system settings</returns>
    public async Task<Settings> Get()
    {
        try
        {
            var result = await HttpHelper.Get<Settings>($"{ServiceBaseUrl}/api/settings");
            if (result.Success == false)
                throw new Exception("Failed to get settings: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get settings: " + ex.Message);
            return null;
        }
    }
}
