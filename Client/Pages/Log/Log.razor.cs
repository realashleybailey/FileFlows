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
        private string lblDownload, lblSearch;
        private string DownloadUrl;

        private SettingsUiModel Settings;
        private LogType LogLevel { get; set; } = LogType.Info;

        private bool SearchVisible;

        private readonly LogSearchModel SearchModel = new()
        {
            Message = string.Empty,
            Type = null,
            ClientUid = null,
            FromDate = DateTime.Today,
            ToDate = DateTime.Today.AddDays(1)
        };

        private bool UsingDatabase =>
            Settings?.DbType == DatabaseType.MySql || Settings?.DbType == DatabaseType.SqlServer; 

#if (!DEMO)
        protected override async Task OnInitializedAsync()
        {
            Settings = (await HttpHelper.Get<SettingsUiModel>("/api/settings/ui-settings")).Data ?? new();


            this.lblSearch = Translater.Instant("Labels.Search");
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
            _ = Refresh();
        }

        async Task Refresh()
        {
            Blocker?.Show();
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
            Blocker?.Hide();
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
    }
}
