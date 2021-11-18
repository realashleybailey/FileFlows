namespace FileFlows.Client.Components.Inputs
{
    using System;
    using System.Threading.Tasks;
    using System.Timers;
    using FileFlows.Client.Helpers;
    using Microsoft.AspNetCore.Components;
    public partial class InputLogView : Input<string>, IDisposable
    {
        [Parameter] public string RefreshUrl { get; set; }

        [Parameter] public int RefreshSeconds { get; set; }

        private Timer RefreshTimer;
        private bool Refreshing = false;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (string.IsNullOrEmpty(RefreshUrl) == false)
            {
                this.RefreshTimer = new Timer();
                this.RefreshTimer.AutoReset = true;
                this.RefreshTimer.Interval = RefreshSeconds > 0 ? RefreshSeconds * 1000 : 10_000;
                this.RefreshTimer.Elapsed += RefreshTimerElapsed;
                this.RefreshTimer.Start();
            }
        }

        public void Dispose()
        {
            if (this.RefreshTimer != null)
            {
                this.RefreshTimer.Stop();
                this.RefreshTimer.Elapsed -= RefreshTimerElapsed;
            }
        }

        private void RefreshTimerElapsed(object sender, EventArgs e)
        {
            if (Refreshing)
                return;
            Refreshing = true;
            Task.Run(async () =>
            {
                try
                {
                    var refreshResult = await HttpHelper.Get<string>(this.RefreshUrl);
                    if (refreshResult.Success == false)
                        return;
                    this.Value = refreshResult.Data;
                    this.StateHasChanged();
                }
                catch (Exception) { }
                finally
                {
                    Refreshing = false;
                }
            });
        }


    }
}