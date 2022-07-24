using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Controller for scheduled tasks
/// </summary>
[Route("/api/task")]
public class TaskController  : ControllerStore<ScheduledTask>
{
    /// <summary>
    /// Get all scheduled tasks configured in the system
    /// </summary>
    /// <returns>A list of all configured scheduled tasks</returns>
    [HttpGet]
    public async Task<IEnumerable<ScheduledTask>> GetAll() => (await GetDataList()).OrderBy(x => x.Name);

    /// <summary>
    /// Get scheduled task
    /// </summary>
    /// <param name="uid">The UID of the scheduled task to get</param>
    /// <returns>The scheduled task instance</returns>
    [HttpGet("{uid}")]
    public Task<ScheduledTask> Get(Guid uid) => GetByUid(uid);

    /// <summary>
    /// Get a scheduled task by its name, case insensitive
    /// </summary>
    /// <param name="name">The name of the scheduled task</param>
    /// <returns>The scheduled task instance if found</returns>
    [HttpGet("name/{name}")]
    public async Task<ScheduledTask?> GetByName(string name)
    {
        if (DbHelper.UseMemoryCache == false)
            return await DbHelper.GetByName<ScheduledTask>(name);
        
        var list = await GetData();
        name = name.ToLower().Trim();
        return list.Values.FirstOrDefault(x => x.Name.ToLower() == name);
    }

    /// <summary>
    /// Saves a scheduled task
    /// </summary>
    /// <param name="scheduledTask">The scheduled task to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public Task<ScheduledTask> Save([FromBody] ScheduledTask scheduledTask) => Update(scheduledTask, checkDuplicateName: true);

    /// <summary>
    /// Delete scheduled tasks from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public Task Delete([FromBody] ReferenceModel<Guid> model) => DeleteAll(model);
}