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
        private string lblDownload;
        private string DownloadUrl;

        private LogType LogLevel { get; set; } = LogType.Info;

#if (!DEMO)
        protected override void OnInitialized()
        {
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
            var response = await HttpHelper.Get<string>("/api/log?logLevel=" + LogLevel);
            if (response.Success)
            {
                this.LogText = response.Data;
                this.StateHasChanged();
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
    }
}