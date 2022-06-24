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


    SearchPane SearchPane { get; set; }

    private LogType LogLevel { get; set; } = LogType.Info;

    private Dictionary<Guid, string> Nodes = new();

    private readonly LogSearchModel SearchModel = new()
    {
        Message = string.Empty,
        ClientUid = null,
        Type = LogType.Info,
        TypeIncludeHigherSeverity = true
    };

#if (!DEMO)
    protected override async Task OnInitializedAsync()
    {
        SearchModel.FromDate = DateRangeHelper.LiveStart;
        SearchModel.ToDate = DateRangeHelper.LiveEnd;
        
        var nodeResult = await HttpHelper.Get<List<ProcessingNode>>("/api/node");
        if (nodeResult.Success && nodeResult.Data != null)
            Nodes = nodeResult.Data.Where(x => x.Uid != new Guid("bf47da28-051e-452e-ad21-c6a3f477fea9")).ToDictionary(x => x.Uid,
                x => x.Name);

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
        if (App.Instance.FileFlowsSystem.ExternalDatabase)
        {
            if (SearchModel.ToDate != DateRangeHelper.LiveEnd || SearchModel.FromDate != DateRangeHelper.LiveStart)
                return;
        }
        
        _ = Refresh();
    }

    async Task Search()
    {
        Blocker.Show(lblSearching);
        await Refresh();
        Blocker.Hide();
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
