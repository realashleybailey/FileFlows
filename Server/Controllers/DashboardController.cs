using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using FileFlows.Shared.Portlets;
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
    public async Task<IEnumerable<Dashboard>> GetAll()
    {
        var dashboards = (await GetDataList()).OrderBy(x => x.Name.ToLower()).ToList();
        if (dashboards.Any() == false)
        {
            // add default
        }
        return dashboards;
    }

    /// <summary>
    /// Get a list of all dashboards
    /// </summary>
    /// <returns>all dashboards in the system</returns>
    [HttpGet("list")]
    public async Task<IEnumerable<ListOption>> ListAll()
    {
        var dashboards = (await GetDataList()).OrderBy(x => x.Name.ToLower()).Select(x => new ListOption
        {
            Label = x.Name,
            Value = x.Uid
        }).ToList();
        // add default
        dashboards.Insert(0, new ()
        {
            Label = Dashboard.DefaultDashboardName,
            Value = Dashboard.DefaultDashboardUid
        });
        return dashboards;
    }

    /// <summary>
    /// Get a dashboard
    /// </summary>
    /// <param name="uid">The UID of the dashboard</param>
    /// <returns>The dashboard instance</returns>
    [HttpGet("{uid}/portlets")]
    public async Task<IEnumerable<PortletUiModel>> Get(Guid uid)
    {
        var db = await GetByUid(uid);
        if ((db == null || db.Uid == Guid.Empty) && uid == Dashboard.DefaultDashboardUid)
            db = Dashboard.GetDefaultDashboard(DbHelper.UseMemoryCache == false);
        else if (db == null)
            throw new Exception("Dashboard not found");
        List<PortletUiModel> portlets = new List<PortletUiModel>();
        foreach (var p in db.Portlets)
        {
            try
            {
                var pd = PortletDefinition.GetDefinition(p.PortletDefinitionUid);
                PortletUiModel pui = new()
                {
                    X = p.X,
                    Y = p.Y,
                    Height = p.Height,
                    Width = p.Width,
                    Uid = p.PortletDefinitionUid,

                    Flags = pd.Flags,
                    Name = pd.Name,
                    Type = pd.Type,
                    Url = pd.Url,
                    Icon = pd.Icon
                };
                #if(DEBUG)
                pui.Url = "http://localhost:6868" + pui.Url;
                #endif
                portlets.Add(pui);
            }
            catch (Exception ex)
            {
                // can throw if portlet definition is not found
                Logger.Instance.WLog("Portlet definition not found: " + p.PortletDefinitionUid);
            }
        }

        return portlets;
    }

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

    /// <summary>
    /// Saves a dashboard
    /// </summary>
    /// <param name="uid">The UID of the dashboard</param>
    /// <param name="portlets">The portlets to save</param>
    /// <returns>The saved dashboard</returns>
    [HttpPut("{uid}")]
    public async Task<Dashboard> Save([FromRoute] Guid uid, [FromBody] List<Portlet> portlets)
    {
        var dashboard = await GetByUid(uid);
        if (dashboard == null)
            throw new Exception("Dashboard not found");
        dashboard.Portlets = portlets ?? new List<Portlet>();
        return await Save(dashboard);
    }
}