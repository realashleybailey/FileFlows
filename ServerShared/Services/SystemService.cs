using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FileFlows.ServerShared.Services;
    
using FileFlows.Shared.Helpers;

/// <summary>
/// An interface of the System Service
/// </summary>
public interface ISystemService
{
    /// <summary>
    /// Gets the version from the server
    /// </summary>
    /// <returns>the server version</returns>
    Task<Version> GetVersion();
    /// <summary>
    /// Gets the node updater binary
    /// </summary>
    /// <returns>the node updater binary</returns>
    Task<byte[]> GetNodeUpdater();
    /// <summary>
    /// Gets an node update available
    /// </summary>
    /// <param name="version">the current version of the node</param>
    /// <returns>if there is a node update available, returns the update</returns>
    Task<byte[]> GetNodeUpdateIfAvailable(string version);
}

/// <summary>
/// A System Service
/// </summary>
public class SystemService : Service, ISystemService
{

    /// <summary>
    /// Gets or sets the loader for SystemService
    /// </summary>
    public static Func<ISystemService> Loader { get; set; }

    /// <summary>
    /// Loads an instance of SystemService
    /// </summary>
    /// <returns>an instance of SystemService</returns>
    public static ISystemService Load()
    {
        if (Loader == null)
            return new SystemService();
        return Loader.Invoke();
    }

    /// <summary>
    /// Gets the version from the server
    /// </summary>
    /// <returns>the server version</returns>
    public async Task<Version> GetVersion()
    {
        try
        {
            var result = await HttpHelper.Get<string>($"{ServiceBaseUrl}/api/system/version");
            if (result.Success == false)
                throw new Exception("Failed to get version: " + result.Body);
            return Version.Parse(result.Data);
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get version: " + ex.Message);
            return new Version(0, 0, 0, 0);
        }
    }
    
    /// <summary>
    /// Gets the node updater binary
    /// </summary>
    /// <returns>the node updater binary</returns>
    public async Task<byte[]> GetNodeUpdater()
    {
        try
        {
            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var result = await HttpHelper.Get<byte[]>($"{ServiceBaseUrl}/api/system/node-updater?windows={windows}");
            if (result.Success == false)
                throw new Exception("Failed to get update: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get update: " + ex.Message);
            return new byte[] { };
        }
    }

    /// <summary>
    /// Gets an node update available
    /// </summary>
    /// <param name="version">the current version of the node</param>
    /// <returns>if there is a node update available, returns the update</returns>
    public async Task<byte[]> GetNodeUpdateIfAvailable(string version)
    {
        try
        {
            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var result = await HttpHelper.Get<byte[]>($"{ServiceBaseUrl}/api/system/node-updater-available?version={version}&windows={windows}");
            if (result.Success == false)
                throw new Exception("Failed to get update: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get update: " + ex.Message);
            return new byte[] { };
        }
    }
}
