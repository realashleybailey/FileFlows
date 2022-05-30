namespace FileFlows.Server.Controllers;

using Microsoft.AspNetCore.Mvc;
using FileFlows.Shared.Models;

/// <summary>
/// Script controller
/// </summary>
[Route("/api/script")]
public class ScriptController : ControllerStore<Script>
{
    /// <summary>
    /// Gets all scripts in the system
    /// </summary>
    /// <returns>a list of all scripts</returns>
    [HttpGet]
    public async Task<IEnumerable<Script>> GetAll() => (await GetDataList()).OrderBy(x => x.Name);


    /// <summary>
    /// Get a script
    /// </summary>
    /// <param name="uid">The UID of the script</param>
    /// <returns>the script instance</returns>
    [HttpGet("{uid}")]
    public Task<Script> Get(Guid uid) => GetByUid(uid);

    /// <summary>
    /// Gets the code for a script
    /// </summary>
    /// <param name="uid">The UID of the script</param>
    /// <returns>the code for a script</returns>
    [HttpGet("{uid}/code")]
    public async Task<string> GetCode(Guid uid) => (await GetByUid(uid))?.Code ?? string.Empty;

    /// <summary>
    /// Saves a script
    /// </summary>
    /// <param name="script">The script to save</param>
    /// <returns>the saved script instance</returns>
    [HttpPost]
    public Task<Script> Save([FromBody] Script script)
    {
        return base.Update(script, checkDuplicateName: true);
    }

    /// <summary>
    /// Delete scripts from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public Task Delete([FromBody] ReferenceModel model) => DeleteAll(model);
}
