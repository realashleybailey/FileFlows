using System.Text.Encodings.Web;
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
    /// <param name="name">The name of the script</param>
    /// <returns>the script</returns>
    Task<Script> Get(string name);
    
    /// <summary>
    /// Gets or sets a function used to load new instances of the service
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <returns>the script code</returns>
    Task<string> GetCode(string name);
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
    /// <param name="name">The name of the script</param>
    /// <returns>the script</returns>
    public async Task<Script> Get(string name)
    {
        try
        {
            string encoded = UrlEncoder.Create().Encode(name);
            var result = await HttpHelper.Get<Script>($"{ServiceBaseUrl}/api/script/{encoded}");
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
    /// Gets or sets a function used to load new instances of the service
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <returns>the script code</returns>
    public async Task<string> GetCode(string name)
    {
        try
        {
            string encoded = UrlEncoder.Create().Encode(name);
            var result = await HttpHelper.Get<string>($"{ServiceBaseUrl}/api/script/{encoded}/code");
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