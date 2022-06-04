using FileFlows.Plugin;

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
    
    
    
    /// <summary>
    /// Exports a script
    /// </summary>
    /// <param name="uid">The UID of the script</param>
    /// <returns>A download response of the script</returns>
    [HttpGet("export/{uid}")]
    public async Task<IActionResult> Export([FromRoute] Guid uid)
    {
        var script = await GetByUid(uid);
        if (script == null)
            return NotFound();
        string json = JsonSerializer.Serialize(script, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        byte[] data = System.Text.UTF8Encoding.UTF8.GetBytes(json);
        return File(data, "application/octet-stream", script.Name + ".json");
    }

    /// <summary>
    /// Imports a script
    /// </summary>
    /// <param name="json">The json data to import</param>
    /// <returns>The newly import script</returns>
    [HttpPost("import")]
    public async Task<Script> Import([FromBody] string json)
    {
        Script? script = JsonSerializer.Deserialize<Script>(json);
        if (string.IsNullOrWhiteSpace(script?.Code))
            throw new Exception("Invalid script");
        
        // will throw if any errors
        new ScriptParser().Parse(script.Name ?? string.Empty, script.Code);
        
        // reparse with new UIDs
        script.Uid = Guid.Empty;
        script.DateModified = DateTime.Now;
        script.DateCreated = DateTime.Now;
        script.Name = await GetNewUniqueName(script.Name);
        return await Update(script);
    }

}
