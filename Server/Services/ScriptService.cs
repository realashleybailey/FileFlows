using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// A service used to get script data from the FileFlows server
/// </summary>
public class ScriptService:IScriptService
{
    /// <summary>
    /// Get a script
    /// </summary>
    /// <param name="uid">The UID identifying the script</param>
    /// <returns>the script</returns>
    public Task<Script> Get(Guid uid) => new ScriptController().Get(uid);

    /// <summary>
    /// Gets or sets a function used to load new instances of the service
    /// </summary>
    public Task<string> GetCode(Guid uid) => new ScriptController().GetCode(uid);
}