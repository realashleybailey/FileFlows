namespace FileFlows.Server.Services;

using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// An instance of the Settings Service which allows accessing of the system settings
/// </summary>
public class SettingsService : ISettingsService
{
    /// <summary>
    /// Gets the system settings
    /// </summary>
    /// <returns>the system settings</returns>
    public Task<Settings> Get() => new SettingsController().Get();

    /// <summary>
    /// Gets the file flows status
    /// </summary>
    /// <returns>the file flows status</returns>
    public Task<FileFlowsStatus> GetFileFlowsStatus() =>
        Task.FromResult(new SettingsController().GetFileFlowsStatus());
}