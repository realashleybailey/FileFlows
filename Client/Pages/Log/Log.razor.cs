using BlazorDateRangePicker;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages
{
    using FileFlows.Shared.Helpers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using FileFlows.Client.Components;
    using System.Timers;
    using FileFlows.Plugin;

    public partial class Log : ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        private string LogText { get; set; }
        private string lblDownload, lblSearch, lblSearching, lblAutoRefresh;
        private string DownloadUrl;

        private bool AutoRefresh { get; set; } = true;

        private SettingsUiModel Settings;
        private LogType LogLevel { get; set; } = LogType.Info;

        private Dictionary<string, DateRange> DateRanges;

        private bool SearchVisible;

        private readonly LogSearchModel SearchModel = new()
        {
            Message = string.Empty,
            ClientUid = null,
            Type = LogType.Info,
            TypeIncludeHigherSeverity = true,
            FromDate = DateTime.Today,
            ToDate = DateTime.Today.AddDays(1)
        };

        private bool UsingDatabase =>
            Settings?.DbType == DatabaseType.MySql || Settings?.DbType == DatabaseType.SqlServer; 

#if (!DEMO)
        protected override async Task OnInitializedAsync()
        {
            Settings = (await HttpHelper.Get<SettingsUiModel>("/api/settings/ui-settings")).Data ?? new();

             DateRanges = new Dictionary<string, DateRange> {
                { 
                    Translater.Instant("Labels.DateRanges.Today"), new DateRange
                    {
                        Start = DateTime.Today,
                        End =  DateTime.Today.AddDays(1).AddTicks(-1)
                    }
                },
                { 
                    Translater.Instant("Labels.DateRanges.Yesterday"), new DateRange
                    {
                        Start = DateTime.Today.AddDays(-1),
                        End =  DateTime.Today.AddTicks(-1)
                    }
                },
                { 
                    Translater.Instant("Labels.DateRanges.Last24Hours"), new DateRange
                    {
                        Start = DateTime.Now.AddDays(-1),
                        End =  DateTime.Now.AddHours(1)
                    }
                },
                { 
                    Translater.Instant("Labels.DateRanges.Last3Days"), new DateRange
                    {
                        Start = DateTime.Now.AddDays(-3),
                        End =  DateTime.Now.AddHours(1)
                    }
                },
            };
            
            this.lblSearch = Translater.Instant("Labels.Search");
            this.lblSearching = Translater.Instant("Labels.Searching");
            this.lblDownload = Translater.Instant("Labels.Download");
            this.lblAutoRefresh = Translater.Instant("Pages.Log.Labels.AutoRefresh");
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
            if (AutoRefresh == false)
                return;
            
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
            if (UsingDatabase)
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

        void ToggleSearch()
        {
            SearchVisible = !SearchVisible;
            this.StateHasChanged();
        }
        
        
        public void OnRangeSelect(DateRange range)
        {
            SearchModel.FromDate = range.Start.Date;
            SearchModel.ToDate = range.End.Date;
        }
    }
}
