namespace FileFlow.Client.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Timers;
    using FileFlow.Client.Components;
    using FileFlow.Client.Components.Dialogs;
    using FileFlow.Client.Helpers;
    using FileFlow.Shared;
    using FileFlow.Shared.Models;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using Radzen;

    public partial class Dashboard : ComponentBase, IDisposable
    {
        const string ApIUrl = "/api/worker";
        private bool Refreshing = false;
        public readonly List<FlowWorkerStatus> Workers = new List<FlowWorkerStatus>();

        public readonly List<LibraryFile> Upcoming = new List<LibraryFile>();
        public readonly List<LibraryFile> Finished = new List<LibraryFile>();
        private bool _needsRendering = false;

        [Inject] private IJSRuntime jSRuntime { get; set; }
        [Inject] public NotificationService NotificationService { get; set; }
        [CascadingParameter] public Blocker Blocker { get; set; }
        [CascadingParameter] Editor Editor { get; set; }

        private IJSObjectReference jsFunctions;

        private string lblLog, lblCancel, lblWaiting, lblCurrentStep, lblFile, lblOverall, lblCurrent, lblProcessingTime, lblUpcoming, lblRecentlyFinished;
        private Timer AutoRefreshTimer;
        protected override async Task OnInitializedAsync()
        {
            AutoRefreshTimer = new Timer();
            AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed;
            AutoRefreshTimer.Interval = 5_000;
            AutoRefreshTimer.AutoReset = true;
            AutoRefreshTimer.Start();
            lblLog = Translater.Instant("Labels.Log");
            lblCancel = Translater.Instant("Labels.Cancel");
            lblWaiting = Translater.Instant("Pages.Dashboard.Messages.Waiting");
            lblCurrentStep = Translater.Instant("Pages.Dashboard.Fields.CurrentStep");
            lblFile = Translater.Instant("Pages.Dashboard.Fields.File");
            lblOverall = Translater.Instant("Pages.Dashboard.Fields.Overall");
            lblCurrent = Translater.Instant("Pages.Dashboard.Fields.Current");
            lblProcessingTime = Translater.Instant("Pages.Dashboard.Fields.ProcessingTime");
            lblUpcoming = Translater.Instant("Pages.Dashboard.Fields.Upcoming");
            lblRecentlyFinished = Translater.Instant("Pages.Dashboard.Fields.RecentlyFinished");
            jsFunctions = await jSRuntime.InvokeAsync<IJSObjectReference>("import", "./scripts/Dashboard.js");
            await this.Refresh();
        }


        public void Dispose()
        {
            Logger.Instance.DLog("Disposing the dashboard!");
            if (jsFunctions != null)
                _ = jsFunctions.InvokeVoidAsync("DestroyAllCharts", this.Workers);
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
            if (Refreshing)
                return;
            Refreshing = true;
            try
            {
                var result = await HttpHelper.Get<List<FlowWorkerStatus>>(ApIUrl);
                if (result.Success)
                {
                    this.Workers.Clear();
                    if (result.Data.Any())
                    {
                        this.Workers.AddRange(result.Data);
                    }
                    await WaitForRender();
                    await jsFunctions.InvokeVoidAsync("InitChart", this.Workers, this.lblOverall, this.lblCurrent);
                }

                var upcomingResult = await HttpHelper.Get<List<LibraryFile>>("/api/library-file/upcoming");
                if (upcomingResult.Success)
                {
                    this.Upcoming.Clear();
                    if (upcomingResult.Data.Any())
                        this.Upcoming.AddRange(upcomingResult.Data);

                    this.StateHasChanged();
                }

                var finishedResult = await HttpHelper.Get<List<LibraryFile>>("/api/library-file/recently-finished");
                if (finishedResult.Success)
                {
                    this.Finished.Clear();
                    if (finishedResult.Data.Any())
                        this.Finished.AddRange(finishedResult.Data);

                    this.StateHasChanged();
                }


            }
            catch (Exception)
            {
            }
            finally
            {
                Refreshing = false;
            }
        }


        private async Task WaitForRender()
        {
            _needsRendering = true;
            StateHasChanged();
            while (_needsRendering)
            {
                await Task.Delay(50);
            }
        }

        protected override void OnAfterRender(bool firstRender)
        {
            _needsRendering = false;
        }

        private async Task ShowFileInfo(LibraryFile file)
        {
            await Helpers.LibraryFileEditor.Open(Blocker, NotificationService, Editor, file);

        }

        private async Task LogClicked(FlowWorkerStatus worker)
        {
            Blocker.Show();
            string log = string.Empty;
            string url = $"{ApIUrl}/{worker.Uid}/log";
            try
            {
                var logResult = await HttpHelper.Get<string>(url);
                if (logResult.Success == false || string.IsNullOrEmpty(logResult.Data))
                {
                    NotificationService.Notify(NotificationSeverity.Error, Translater.Instant("Pages.Dashboard.ErrorMessages.LogFailed"));
                    return;
                }
                log = logResult.Data;
            }
            finally
            {
                Blocker.Hide();
            }

            List<ElementField> fields = new List<ElementField>();
            fields.Add(new ElementField
            {
                InputType = FileFlow.Plugin.FormInputType.LogView,
                Name = "Log",
                Parameters = new Dictionary<string, object> {
                    { nameof(Components.Inputs.InputLogView.RefreshUrl), url },
                    { nameof(Components.Inputs.InputLogView.RefreshSeconds), 3 },
                }
            });

            await Editor.Open("Pages.Dashboard", worker.CurrentFile, fields, new { Log = log }, large: true, readOnly: true);
        }

        private async Task CancelClicked(FlowWorkerStatus worker)
        {
            if (await Confirm.Show("Labels.Cancel",
                Translater.Instant("Pages.Dashboard.Messages.CancelMesssage", worker)) == false)
                return; // rejected the confirm

            Blocker.Show();
            try
            {
                await HttpHelper.Delete($"{ApIUrl}/{worker.Uid}");
                await Task.Delay(1000);
                await Refresh();
            }
            finally
            {
                Blocker.Hide();
            }
        }
    }
}