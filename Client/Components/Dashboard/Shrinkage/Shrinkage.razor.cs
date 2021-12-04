namespace FileFlows.Client.Components.Dashboard
{
    using FileFlows.Shared;
    using FileFlows.Shared.Helpers;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using System;
    using System.Threading.Tasks;
    using System.Timers;

    public partial class Shrinkage:ComponentBase
    {
        [CascadingParameter] Pages.Dashboard Dashboard { get; set; }
        private string Uid = Guid.NewGuid().ToString();


        private FileFlows.Shared.Models.ShrinkageData Data;
        private Timer AutoRefreshTimer;
        private bool HasData = false;

        private string lblOriginalSize, lblFinalSize, lblSavings, lblShrinkageTitle;

        protected override async Task OnInitializedAsync()
        {
            lblOriginalSize = Translater.Instant("Pages.Dashboard.Labels.OriginalSize");
            lblFinalSize = Translater.Instant("Pages.Dashboard.Labels.FinalSize");
            lblSavings = Translater.Instant("Pages.Dashboard.Labels.Savings");
            lblShrinkageTitle = Translater.Instant("Pages.Dashboard.Labels.ShrinkageTitle");

            AutoRefreshTimer = new Timer();
            AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed;
            AutoRefreshTimer.Interval = 60_000;
            AutoRefreshTimer.AutoReset = true;
            AutoRefreshTimer.Start();
            await Refresh();
        }
        public void Dispose()
        {
            Logger.Instance.DLog("Disposing the shrinkage");
            if (Dashboard.jsFunctions != null)
            {
                try
                {
                    _ = Dashboard.jsFunctions.InvokeVoidAsync("DestroyChart", this.Uid);
                }
                catch (Exception) { }
            }
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
#if (DEMO)
            Data = new FileFlows.Shared.Models.ShrinkageData { FinalSize = 10_000_000, OriginalSize = 25_000_000 };
#else
            var result = await HttpHelper.Get<FileFlows.Shared.Models.ShrinkageData>("/api/library-file/shrinkage");
            if (result.Success)
            {
                Data = result.Data ?? new FileFlows.Shared.Models.ShrinkageData();
            }
            else if (Data == null)
                Data = new FileFlows.Shared.Models.ShrinkageData();
#endif
            HasData = Data.FinalSize > 0 && Data.OriginalSize > 0;

            try
            {
                await Dashboard.jsFunctions.InvokeVoidAsync("InitPieChart", this.Uid,
                    new[] { Data.FinalSize, Data.OriginalSize - Data.FinalSize },
                    new[] { lblFinalSize, lblSavings }
                );
            }
            catch (Exception) { }
        }

    }
}
