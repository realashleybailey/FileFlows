using System.Timers;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages;

public partial class Dashboard : ComponentBase
{
    private ConfigurationStatus ConfiguredStatus = ConfigurationStatus.Flows | ConfigurationStatus.Libraries;
    [Inject] public IJSRuntime jSRuntime { get; set; }
    [CascadingParameter] public Blocker Blocker { get; set; }
    [CascadingParameter] Editor Editor { get; set; }
    public EventHandler AddPortletEvent { get; set; }

    private string lblAddPortlet;
    
    private List<ListOption> Dashboards;
    private Guid ActiveDashboardUid;
    
    protected override async Task OnInitializedAsync()
    {
#if (DEMO)
        ConfiguredStatus = ConfigurationStatus.Flows | ConfigurationStatus.Libraries;
#else
        ConfiguredStatus = App.Instance.FileFlowsSystem.ConfigurationStatus;
#endif
        lblAddPortlet = Translater.Instant("Pages.Dashboard.Labels.AddPortlet");
        
        if(UsingBasicDashboard == false)
            await LoadDashboards();
    }

    private async Task LoadDashboards()
    {
        if (App.Instance.FileFlowsSystem.Licensed == false)
            return;
        var dbResponse = await HttpHelper.Get<List<ListOption>>("/api/dashboard/list");
        if (dbResponse.Success == false || dbResponse.Data == null)
            return;
        this.Dashboards = dbResponse.Data;
        foreach (var db in this.Dashboards)
            db.Value = Guid.Parse(db.Value.ToString());
        this.Dashboards.Add(new ListOption()
        {
            Label = "Basic Dashboard",
            Value = Guid.Empty
        });
        this.Dashboards = this.Dashboards.OrderBy(x =>
        {
            if ((Guid)x.Value == FileFlows.Shared.Models.Dashboard.DefaultDashboardUid)
                return -2;
            if ((Guid)x.Value == Guid.Empty)
                return -1;
            return 0;
        }).ThenBy(x => x.Label).ToList();
        this.ActiveDashboardUid = (Guid)this.Dashboards[0].Value;
    }


    private bool UsingBasicDashboard => App.Instance.FileFlowsSystem.Licensed == false;



    private async Task AddDashboard()
    {
        string name = await Prompt.Show("New Dashboard", "Enter a name of the new dashboard");
    }

    private async Task DeleteDashboard()
    {
        if (DashboardDeletable == false)
            return;
        bool confirmed = await Confirm.Show("Labels.Delete", "Pages.Dashboard.Messages.DeleteDashboard");
    }

    private bool DashboardDeletable => ActiveDashboardUid != Guid.Empty &&
                                      ActiveDashboardUid != FileFlows.Shared.Models.Dashboard.DefaultDashboardUid;

    private void AddPortlet() => AddPortletEvent?.Invoke(this, new EventArgs());

}