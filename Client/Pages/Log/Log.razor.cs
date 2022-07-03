using System.Reflection;
using BlazorDateRangePicker;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using System.Timers;
using FileFlows.Client.Helpers;

namespace FileFlows.Client.Pages;


public partial class Log : ComponentBase
{
    [CascadingParameter] Blocker Blocker { get; set; }
    [Inject] NavigationManager NavigationManager { get; set; }
    private string LogText { get; set; }
    private string lblDownload, lblSearch, lblSearching;
    private string DownloadUrl;

    private bool Searching = false;

    SearchPane SearchPane { get; set; }

    private LogType LogLevel { get; set; } = LogType.Info;

    private Dictionary<Guid, string> Nodes = new();

    private List<ListOption> LoggingSources = new ();

    private readonly LogSearchModel SearchModel = new()
    {
        Message = string.Empty,
        Source = string.Empty,
        Type = LogType.Info,
        TypeIncludeHigherSeverity = true
    };

#if (!DEMO)
    protected override async Task OnInitializedAsync()
    {
        SearchModel.FromDate = DateRangeHelper.LiveStart;
        SearchModel.ToDate = DateRangeHelper.LiveEnd;

        LoggingSources = (await HttpHelper.Get<List<ListOption>>("/api/log/log-sources")).Data;

        this.lblSearch = Translater.Instant("Labels.Search");
        this.lblSearching = Translater.Instant("Labels.Searching");
        this.lblDownload = Translater.Instant("Labels.Download");
#if (DEBUG)
        this.DownloadUrl = "http://localhost:6868/api/log/download";
#else
        this.DownloadUrl = "/api/log/download";
#endif
        NavigationManager.LocationChanged += NavigationManager_LocationChanged;
        AutoRefreshTimer = new Timer();
        AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed;
        AutoRefreshTimer.Interval = 5_000;
        AutoRefreshTimer.AutoReset = true;
        AutoRefreshTimer.Start();
        _ = Refresh();

    }
    private Timer AutoRefreshTimer;

    private void NavigationManager_LocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        Dispose();
    }

    public void Dispose()
    {
        if (AutoRefreshTimer != null)
        {
            AutoRefreshTimer.Stop();
            AutoRefreshTimer.Elapsed -= AutoRefreshTimerElapsed;
            AutoRefreshTimer.Dispose();
            AutoRefreshTimer = null;
        }
    }
    void AutoRefreshTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if (Searching)
            return;
        
        if (App.Instance.FileFlowsSystem.ExternalDatabase)
        {
            if (SearchModel.ToDate != DateRangeHelper.LiveEnd || SearchModel.FromDate != DateRangeHelper.LiveStart)
                return;
        }
        
        _ = Refresh();
    }

    async Task Search()
    {
        this.Searching = true;
        try
        {
            Blocker.Show(lblSearching);
            await Refresh();
            Blocker.Hide();
        }
        finally
        {
            this.Searching = false;
        }
    }

    async Task Refresh()
    {
        if (App.Instance.FileFlowsSystem.ExternalDatabase)
        {
            var response = await HttpHelper.Post<string>("/api/log/search", SearchModel);
            if (response.Success)
            {
                this.LogText = response.Data;
                this.StateHasChanged();
            }
        }
        else
        {
            var response = await HttpHelper.Get<string>("/api/log?logLevel=" + LogLevel);
            if (response.Success)
            {
                this.LogText = response.Data;
                this.StateHasChanged();
            }
        }
    }
#endif

    async Task ChangeLogType(ChangeEventArgs args)
    {
#if (DEMO)
        return;
#endif
        this.LogLevel = (LogType)int.Parse(args.Value.ToString());
        await Refresh();
    }

    
    public void OnRangeSelect(DateRange range)
    {
        SearchModel.FromDate = range.Start.Date;
        SearchModel.ToDate = range.End.Date;
    }
}
