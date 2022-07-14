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
    [Inject] private Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; }
    [CascadingParameter] public Blocker Blocker { get; set; }
    [CascadingParameter] Editor Editor { get; set; }
    public EventHandler AddWidgetEvent { get; set; }

    private string lblAddWidget;
    
    private List<ListOption> Dashboards;

    private Guid _ActiveDashboardUid;

    /// <summary>
    /// Gets the UID of the active dashboard
    /// </summary>
    public Guid ActiveDashboardUid
    {
        get => _ActiveDashboardUid;
        private set
        {
            _ActiveDashboardUid = value;
            _ = LocalStorage.SetItemAsync("ACTIVE_DASHBOARD", value);
        }
    }


    private bool Unlicensed => App.Instance.FileFlowsSystem.Licensed == false;
    
    protected override async Task OnInitializedAsync()
    {
#if (DEMO)
        ConfiguredStatus = ConfigurationStatus.Flows | ConfigurationStatus.Libraries;
#else
        ConfiguredStatus = App.Instance.FileFlowsSystem.ConfigurationStatus;
#endif
        lblAddWidget = Translater.Instant("Pages.Dashboard.Labels.AddWidget");
        
        if(Unlicensed == false)
            await LoadDashboards();
    }

    private async Task LoadDashboards()
    {
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
        SortDashboards();
        var lsActiveDashboard = await LocalStorage.GetItemAsync<Guid?>("ACTIVE_DASHBOARD");
        if (lsActiveDashboard != null &&  this.Dashboards.Any(x => ((Guid)x.Value) == lsActiveDashboard))
        {
            this.ActiveDashboardUid = lsActiveDashboard.Value;
        }
        else
        {
            this.ActiveDashboardUid = (Guid)this.Dashboards[0].Value;
        }
    }

    private void SortDashboards()
    {
        this.Dashboards = this.Dashboards.OrderBy(x =>
        {
            if ((Guid)x.Value == FileFlows.Shared.Models.Dashboard.DefaultDashboardUid)
                return -2;
            if ((Guid)x.Value == Guid.Empty)
                return -1;
            return 0;
        }).ThenBy(x => x.Label).ToList();
    }



    private async Task AddDashboard()
    {
        string name = await Prompt.Show("New Dashboard", "Enter a name of the new dashboard");
        if (string.IsNullOrWhiteSpace(name))
            return; // was canceled
        this.Blocker.Show();
        try
        {
            var newDashboardResult = await HttpHelper.Put<FileFlows.Shared.Models.Dashboard>("/api/dashboard", new { Name = name });
            if (newDashboardResult.Success == false)
            {
                string error = newDashboardResult.Body?.EmptyAsNull() ?? "Pages.Dashboard.ErrorMessages.FailedToCreate";
                Toast.ShowError(error);
                return;
            }
            this.Dashboards.Add(new ()
            {
                Label = newDashboardResult.Data.Name,
                Value = newDashboardResult.Data.Uid
            });
            SortDashboards();
            this.ActiveDashboardUid = newDashboardResult.Data.Uid;
        }
        finally
        {
            this.Blocker.Hide();
        }
    }

    private async Task DeleteDashboard()
    {
        if (DashboardDeletable == false)
            return;
        bool confirmed = await Confirm.Show("Labels.Delete", "Pages.Dashboard.Messages.DeleteDashboard");
        if (confirmed == false)
            return;
        Blocker.Show();
        try
        {
            await HttpHelper.Delete("/api/dashboard/" + ActiveDashboardUid);
            this.Dashboards.RemoveAll(x => (Guid)x.Value == ActiveDashboardUid);
            this.ActiveDashboardUid = (Guid)this.Dashboards[0].Value;
        }
        finally
        {
            Blocker.Hide();
        }
    }

    private bool DashboardDeletable => ActiveDashboardUid != Guid.Empty &&
                                      ActiveDashboardUid != FileFlows.Shared.Models.Dashboard.DefaultDashboardUid;

    private void AddWidget() => AddWidgetEvent?.Invoke(this, new EventArgs());

}