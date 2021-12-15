namespace FileFlows.Client.Components.Dashboard
{
    using FileFlows.Shared;
    using FileFlows.Shared.Helpers;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using System;
    using System.Threading.Tasks;
    using System.Timers;


    public partial class LibraryFilesSummary:ComponentBase
    {
        [CascadingParameter] Pages.Dashboard Dashboard { get; set; }
        [CascadingParameter] Editor Editor { get; set; }

        [Parameter] public bool Completed { get; set; }

        private string Uid = Guid.NewGuid().ToString();

        private List<LibraryFile> Data;
        private Timer AutoRefreshTimer;

        private bool HasRendered = false;
        private string lblRecentlyFinished;

        protected override async Task OnInitializedAsync()
        {
            lblRecentlyFinished = Translater.Instant("Pages.Dashboard.Fields." + (Completed ? "RecentlyFinished" : "Upcoming"));
            AutoRefreshTimer = new Timer();
            AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed;
            AutoRefreshTimer.Interval = 5_000;
            AutoRefreshTimer.AutoReset = true;
            AutoRefreshTimer.Start();
            await Refresh();
        }
        public void Dispose()
        {
            Logger.Instance.DLog("Disposing the libraryfilessummary");
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

        protected override void OnAfterRender(bool firstRender)
        {
            this.HasRendered = true;
            base.OnAfterRender(firstRender);
        }

        private async Task WaitUntilHasRendered()
        {
            while (this.HasRendered == false)
                await Task.Delay(50);
        }


        private async Task Refresh()
        {
#if (DEMO)
            Data = new FileFlows.Shared.Models.ShrinkageData { FinalSize = 10_000_000, OriginalSize = 25_000_000 };
#else
            var result = await GetLibraryFiles("/api/library-file/" + (Completed ? "recently-finished" : "upcoming"));
            if (result.Success)
            {
                Data = result.Data ?? new();
            }
            else if (Data == null)
                Data = new ();
#endif
            this.StateHasChanged();
        }

        private async Task<RequestResult<List<LibraryFile>>> GetLibraryFiles(string url)
        {
#if (DEMO)
            var data = Enumerable.Range(1, 10).Select(x => new LibraryFile
            {
                Name = RecentlyFinished ? $"Completed File {x}.mkv" : $"Upcoming File {x}.mkv",
                RelativePath = RecentlyFinished ? $"Completed File {x}.mkv" : $"Upcoming File {x}.mkv",
                ProcessingStarted = DateTime.Now.AddMinutes(-5),
                ProcessingEnded = DateTime.Now
            }).ToList();
            return new RequestResult<List<LibraryFile>> { Success = true, Data = data };
#else
            return await HttpHelper.Get<List<LibraryFile>>(url);
#endif
        }
        private async Task ShowFileInfo(LibraryFile file)
        {
            await Helpers.LibraryFileEditor.Open(Dashboard.Blocker, Dashboard.NotificationService, Editor, file);

        }
    }
}
