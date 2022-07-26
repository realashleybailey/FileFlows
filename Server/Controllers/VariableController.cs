using Microsoft.AspNetCore.Mvc;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Variable Controller
/// </summary>
[Route("/api/variable")]
public class VariableController : ControllerStore<Variable>
{
    /// <summary>
    /// Get all variables configured in the system
    /// </summary>
    /// <returns>A list of all configured variables</returns>
    [HttpGet]
    public async Task<IEnumerable<Variable>> GetAll() => (await GetDataList()).OrderBy(x => x.Name);

    /// <summary>
    /// Get variable
    /// </summary>
    /// <param name="uid">The UID of the variable to get</param>
    /// <returns>The variable instance</returns>
    [HttpGet("{uid}")]
    public Task<Variable> Get(Guid uid) => GetByUid(uid);

    /// <summary>
    /// Get a variable by its name, case insensitive
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <returns>The variable instance if found</returns>
    [HttpGet("name/{name}")]
    public async Task<Variable?> GetByName(string name)
    {
        if (DbHelper.UseMemoryCache == false)
            return await DbHelper.GetByName<Variable>(name);
        
        var list = await GetData();
        name = name.ToLower().Trim();
        return list.Values.FirstOrDefault(x => x.Name.ToLower() == name);
    }

    /// <summary>
    /// Saves a variable
    /// </summary>
    /// <param name="variable">The variable to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public async Task<Variable> Save([FromBody] Variable variable)
    {
        var result = await Update(variable, checkDuplicateName: true);
        Workers.FileFlowTasksWorker.ReloadVariables();
        return result;
    }

    /// <summary>
    /// Delete variables from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
    {
        await DeleteAll(model);
        Workers.FileFlowTasksWorker.ReloadVariables();
    }
}