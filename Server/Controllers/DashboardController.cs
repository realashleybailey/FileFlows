using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Controller for the dashboard
/// </summary>
[Route("/api/dashboard")]
public class DashboardController:ControllerStore<Dashboard>
{
    /// <summary>
    /// Get all dashboards in the system
    /// </summary>
    /// <returns>all dashboards in the system</returns>
    [HttpGet]
    public async Task<IEnumerable<Dashboard>> GetAll() => (await GetDataList()).OrderBy(x => x.Name.ToLower());

    /// <summary>
    /// Get a dashboard
    /// </summary>
    /// <param name="uid">The UID of the dashboard</param>
    /// <returns>The dashboard instance</returns>
    [HttpGet("{uid}")]
    public Task<Dashboard> Get(Guid uid) => GetByUid(uid);

    /// <summary>
    /// Delete a dashboard from the system
    /// </summary>
    /// <param name="uid">The UID of the dashboard to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete("{uid}")]
    public Task Delete([FromRoute] Guid uid) => base.DeleteAll(new[] { uid });
    
    
    /// <summary>
    /// Saves a dashboard
    /// </summary>
    /// <param name="model">The dashboard being saved</param>
    /// <returns>The saved dashboard</returns>
    [HttpPut]
    public async Task<Dashboard> Save([FromBody] Dashboard model)
    {
        if (model == null)
            throw new Exception("No model");

        if (string.IsNullOrWhiteSpace(model.Name))
            throw new Exception("ErrorMessages.NameRequired");
        model.Name = model.Name.Trim();
        bool inUse = await NameInUse(model.Uid, model.Name);
        if (inUse)
            throw new Exception("ErrorMessages.NameInUse");

        return await Update(model);
    }
}