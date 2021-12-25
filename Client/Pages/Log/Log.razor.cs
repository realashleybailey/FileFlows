namespace FileFlows.Client.Pages
{
    using FileFlows.Shared.Helpers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using FileFlows.Client.Components;
    using System.Timers;

    public partial class Log : ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }
        private string LogText { get; set; }
#if (!DEMO)
        private Timer AutoRefreshTimer;

        protected override void OnInitialized()
        {
            AutoRefreshTimer = new Timer();
            AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed;
            AutoRefreshTimer.Interval = 5_000;
            AutoRefreshTimer.AutoReset = true;
            AutoRefreshTimer.Start();
            _ = Refresh();
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
            var response = await HttpHelper.Get<string>("/api/log");
            if (response.Success)
            {
                this.LogText = response.Data;
                this.StateHasChanged();
            }
        }
#endif
    }
}