using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v0.8.0
/// </summary>
public class Upgrade0_8_0
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.8.0 upgrade script");
        settings.LogFileRetention = 2;
        settings.LogDatabaseRetention = 50_000;
    }
}