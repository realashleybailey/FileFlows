namespace FileFlows.Client.Components
{
    using Microsoft.AspNetCore.Components;
    using FileFlows.Client.Shared;
    using System.Timers;
    using System.Threading.Tasks;
    using System;

    public partial class VersionUpdateChecker
    {
        private bool Dismissed { get; set; }
        private bool UpdateAvailable { get; set; }
        private Version LatestVersion { get; set; }
        private Timer AutoRefreshTimer;
        private string lblUpdateAvailable, lblUpdateAvailableSuffix;


        protected override async Task OnInitializedAsync()
        {
            this.LatestVersion = new Version(Globals.Version);
            AutoRefreshTimer = new Timer();
            AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed;
            AutoRefreshTimer.Interval = 3600 * 1000; // once an hour, dont need to hammer it
            AutoRefreshTimer.AutoReset = true;
            AutoRefreshTimer.Start();
            await Refresh();
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

        private async Task Refresh()
        {
#if (DEMO || DEBUG)
#else
            var result = await HttpHelper.Get<string>("/api/settings/check-update-available");
            if (result.Success && string.IsNullOrWhiteSpace(result.Data) == false)
            {
                try
                {
                    // new version 
                    LatestVersion = new Version(result.Data);
                    Dismissed = false;
                    UpdateAvailable = true;

                    string versionString = LatestVersion.ToString();
                    string lbl = Translater.Instant("Labels.UpdateAvailable", new { version = versionString });
                    int index = lbl.IndexOf(versionString);
                    if (lbl.EndsWith(versionString))
                    {
                        lblUpdateAvailable = lbl.Substring(0, index);
                        lblUpdateAvailableSuffix = String.Empty;
                    } 
                    else
                    {
                        this.lblUpdateAvailable = lbl.Substring(0, index);
                        this.lblUpdateAvailableSuffix = lbl.Substring(index + versionString.Length);
                    }
                    this.StateHasChanged();
                }                
                catch (Exception ex) { }
            }
#endif
        }

        void Dismiss()
        {
            this.Dismissed = true;
            this.StateHasChanged();
        }

    }
}
