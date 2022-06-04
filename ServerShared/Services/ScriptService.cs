using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Script Service interface
/// </summary>
public interface IScriptService
{
    /// <summary>
    /// Get a script
    /// </summary>
    /// <param name="uid">The UID identifying the script</param>
    /// <returns>the script</returns>
    Task<Script> Get(Guid uid);
    
    /// <summary>
    /// Get the code for a script
    /// </summary>
    /// <param name="uid">The UID identifying the script</param>
    /// <returns>the code for the script</returns>
    Task<string> GetCode(Guid uid);
}

/// <summary>
/// A service used to get script data from the FileFlows server
/// </summary>
public class ScriptService:Service, IScriptService
{

    /// <summary>
    /// Gets or sets a function used to load new instances of the service
    /// </summary>
    public static Func<IScriptService> Loader { get; set; }

    /// <summary>
    /// Loads an instance of the script service
    /// </summary>
    /// <returns>an instance of the script service</returns>
    public static IScriptService Load()
    {
        if (Loader == null)
            return new ScriptService();
        return Loader.Invoke();
    }

    /// <summary>
    /// Get a script
    /// </summary>
    /// <param name="uid">The UID identifying the script</param>
    /// <returns>the script</returns>
    public async Task<Script> Get(Guid uid)
    {
        try
        {
            var result = await HttpHelper.Get<Script>($"{ServiceBaseUrl}/api/script/{uid}");
            if (result.Success == false)
                throw new Exception(result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get script: " + ex.Message);
            return new Script { Code = string.Empty, Name = string.Empty };
        }
    }

    /// <summary>
    /// Get the code for a script
    /// </summary>
    /// <param name="uid">The UID identifying the script</param>
    /// <returns>the code for the script</returns>
    public async Task<string> GetCode(Guid uid)
    {
        try
        {
            var result = await HttpHelper.Get<string>($"{ServiceBaseUrl}/api/script/{uid}/code");
            if (result.Success == false)
                throw new Exception(result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get script code: " + ex.Message);
            return string.Empty;
        }
    }
}