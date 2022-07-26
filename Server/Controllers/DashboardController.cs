using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using FileFlows.Shared.Widgets;
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
    [HttpGet("{uid}/Widgets")]
    public async Task<IEnumerable<WidgetUiModel>> Get(Guid uid)
    {
        var db = await GetByUid(uid);
        if ((db == null || db.Uid == Guid.Empty) && uid == Dashboard.DefaultDashboardUid)
            db = Dashboard.GetDefaultDashboard(DbHelper.UseMemoryCache == false);
        else if (db == null)
            throw new Exception("Dashboard not found");
        List<WidgetUiModel> Widgets = new List<WidgetUiModel>();
        foreach (var p in db.Widgets)
        {
            try
            {
                var pd = WidgetDefinition.GetDefinition(p.WidgetDefinitionUid);
                WidgetUiModel pui = new()
                {
                    X = p.X,
                    Y = p.Y,
                    Height = p.Height,
                    Width = p.Width,
                    Uid = p.WidgetDefinitionUid,

                    Flags = pd.Flags,
                    Name = pd.Name,
                    Type = pd.Type,
                    Url = pd.Url,
                    Icon = pd.Icon
                };
                #if(DEBUG)
                pui.Url = "http://localhost:6868" + pui.Url;
                #endif
                Widgets.Add(pui);
            }
            catch (Exception)
            {
                // can throw if Widget definition is not found
                Logger.Instance.WLog("Widget definition not found: " + p.WidgetDefinitionUid);
            }
        }

        return Widgets;
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
    /// <param name="Widgets">The Widgets to save</param>
    /// <returns>The saved dashboard</returns>
    [HttpPut("{uid}")]
    public async Task<Dashboard> Save([FromRoute] Guid uid, [FromBody] List<Widget> Widgets)
    {
        var dashboard = await GetByUid(uid);
        if (dashboard == null)
            throw new Exception("Dashboard not found");
        dashboard.Widgets = Widgets ?? new List<Widget>();
        return await Save(dashboard);
    }
}