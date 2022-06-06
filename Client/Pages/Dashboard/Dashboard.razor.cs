namespace FileFlows.Client.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Timers;
    using FileFlows.Client.Components;
    using FileFlows.Client.Components.Dialogs;
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using FileFlows.Plugin;

    public partial class Dashboard : ComponentBase, IDisposable
    {
        const string ApIUrl = "/api/worker";
        private bool Refreshing = false;
        public readonly List<FlowExecutorInfo> Workers = new List<FlowExecutorInfo>();

        public readonly List<LibraryFile> Upcoming = new List<LibraryFile>();
        private bool _needsRendering = false;

        private int ConfiguredStatus = 2;
        [Inject] public IJSRuntime jSRuntime { get; set; }
        [CascadingParameter] public Blocker Blocker { get; set; }
        [CascadingParameter] Editor Editor { get; set; }

        public IJSObjectReference jsFunctions;

        private string lblLog, lblCancel, lblWaiting, lblCurrentStep, lblNode, lblFile, lblOverall, lblCurrent, lblProcessingTime, lblWorkingFile, lblUid, lblLibrary, lblPauseLabel;
        private Timer AutoRefreshTimer;

        private SystemInfo SystemInfo = new SystemInfo();
        public delegate void Disposing();
        public event Disposing OnDisposing;
        protected override async Task OnInitializedAsync()
        {
            AutoRefreshTimer = new Timer();
            AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed;
            AutoRefreshTimer.Interval = 5_000;
            AutoRefreshTimer.AutoReset = true;
            AutoRefreshTimer.Start();
#if (DEMO)
            ConfiguredStatus = 2;
#else
            ConfiguredStatus = (await HttpHelper.Get<int>("/api/settings/is-configured")).Data;
#endif
            lblLog = Translater.Instant("Labels.Log");
            lblCancel = Translater.Instant("Labels.Cancel");
            lblWaiting = Translater.Instant("Pages.Dashboard.Messages.Waiting");
            lblCurrentStep = Translater.Instant("Pages.Dashboard.Fields.CurrentStep");
            lblNode= Translater.Instant("Pages.Dashboard.Fields.Node");
            lblFile = Translater.Instant("Pages.Dashboard.Fields.File");
            lblOverall = Translater.Instant("Pages.Dashboard.Fields.Overall");
            lblCurrent = Translater.Instant("Pages.Dashboard.Fields.Current");
            lblUid = Translater.Instant("Pages.Dashboard.Fields.Uid");
            lblProcessingTime = Translater.Instant("Pages.Dashboard.Fields.ProcessingTime");
            lblLibrary = Translater.Instant("Pages.Dashboard.Fields.Library");
            lblWorkingFile = Translater.Instant("Pages.Dashboard.Fields.WorkingFile");
            await GetJsFunctionObject();
            await this.Refresh();
        }

        private System.Threading.Mutex mutexJsFunctions = new ();
        public async Task<IJSObjectReference> GetJsFunctionObject()
        {
            if (jsFunctions != null)
                return jsFunctions;
            mutexJsFunctions.WaitOne();
            if (jsFunctions != null)
                return jsFunctions; // incase was fetched while mutex was locked
            try
            {
                jsFunctions = await jSRuntime.InvokeAsync<IJSObjectReference>("import", "./scripts/Dashboard.js");
                return jsFunctions;
            }
            finally
            {
                mutexJsFunctions.ReleaseMutex();
            }
        }

        public void Dispose()
        {
            OnDisposing?.Invoke();
            if (jsFunctions != null)
            {
                try
                {
                    _ = jsFunctions.InvokeVoidAsync("DestroyAllCharts", this.Workers);
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


        async Task Refresh()
        {
            if (Refreshing)
                return;
            Refreshing = true;
            try
            {
                RequestResult<List<FlowExecutorInfo>> result = null;
                RequestResult<SystemInfo> systemInfoResult = null;
                var tasks = new Task[]
                {
                    Task.Run(async () => result = await GetData()),
                    Task.Run(async () => systemInfoResult = await GetSystemInfo()),
                };
                await Task.WhenAll(tasks);
                if (result.Success)
                {
                    this.Workers.Clear();
                    if (result.Data.Any())
                    {
                        foreach(var worker in result.Data)
                        {
                            if (worker.NodeName == "FileFlowsServer")
                                worker.NodeName = Translater.Instant("Pages.Nodes.Labels.FileFlowsServer");
                        }
                        this.Workers.AddRange(result.Data);
                    }
                    await WaitForRender();
                    try
                    {
                        await jsFunctions.InvokeVoidAsync("InitChart", this.Workers, this.lblOverall, this.lblCurrent);
                    }
                    catch(Exception) { }
                }

                if (systemInfoResult.Success)
                {
                    this.SystemInfo = systemInfoResult.Data;
                    this.lblPauseLabel =
                        Translater.Instant(systemInfoResult.Data.IsPaused ? "Labels.Resume" : "Labels.Pause");
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

        async Task<RequestResult<SystemInfo>> GetSystemInfo()
        {
#if (DEMO)
            var random = new Random(DateTime.Now.Millisecond);
            return new SystemInfo { CpuUsage = random.Next() * 100f, MemoryUsage = random.Next() * 1_000_000_000 };
#else
            return await HttpHelper.Get<SystemInfo>("/api/system/info");
#endif
        }

        async Task<RequestResult<List<FlowExecutorInfo>>> GetData()
        {
#if (DEMO)
            return new RequestResult<List<FlowExecutorInfo>>
            {
                Success = true,
                Data = new List<FlowExecutorInfo>
                {
                    new FlowExecutorInfo
                    {
                        LibraryFile = new LibraryFile { Name = "DemoFile.mkv" },
                        LibraryPath = @"C:\Videos",
                        CurrentPart = 1,
                        CurrentPartName = "Curren Flow Part",
                        CurrentPartPercent = 50,
                        LastUpdate = DateTime.Now,
                        Log = "Test Log",
                        NodeName = "Remote Processing Node",
                        NodeUid = Guid.NewGuid(),
                        Library = new ObjectReference
                        {
                            Name = "Demo Library",
                            Uid = Guid.NewGuid()
                        },
                        RelativeFile = "DemoFile.mkv",
                        StartedAt = DateTime.Now.AddMinutes(-1),
                        TotalParts = 5,
                        WorkingFile = "tempfile.mkv",
                        Uid = Guid.NewGuid()
                    }
                }
            };
#else
            return await HttpHelper.Get<List<FlowExecutorInfo>>(ApIUrl);
#endif
            
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


        private async Task LogClicked(FlowExecutorInfo worker)
        {
            Blocker.Show();
            string log = string.Empty;
            string url = $"{ApIUrl}/{worker.LibraryFile.Uid}/log?lineCount=200";
            try
            {
                var logResult = await GetLog(url);
                if (logResult.Success == false || string.IsNullOrEmpty(logResult.Data))
                {
                    Toast.ShowError( Translater.Instant("Pages.Dashboard.ErrorMessages.LogFailed"));
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
                InputType = FormInputType.LogView,
                Name = "Log",
                Parameters = new Dictionary<string, object> {
                    { nameof(Components.Inputs.InputLogView.RefreshUrl), url },
                    { nameof(Components.Inputs.InputLogView.RefreshSeconds), 3 },
                }
            });

            await Editor.Open("Pages.Dashboard", worker.LibraryFile.Name, fields, new { Log = log }, large: true, readOnly: true);
        }

        private async Task<RequestResult<string>> GetLog(string url)
        {
#if (DEMO)
            return new RequestResult<string>
            {
                Success = true,
                Data = @"2021-11-27 11:46:15.0658 - Debug -> Executing part:
2021-11-27 11:46:15.1414 - Debug -> node: VideoFile
2021-11-27 11:46:15.8442 - Info -> Video Information:
ffmpeg version 4.1.8 Copyright (c) 2000-2021 the FFmpeg developers
  built with gcc 9 (Ubuntu 9.3.0-17ubuntu1~20.04)
  configuration: --disable-debug --disable-doc --disable-ffplay --enable-shared --enable-avresample --enable-libopencore-amrnb --enable-libopencore-amrwb --enable-gpl --enable-libass --enable-fontconfig --enable-libfreetype --enable-libvidstab --enable-libmp3lame --enable-libopus --enable-libtheora --enable-libvorbis --enable-libvpx --enable-libwebp --enable-libxcb --enable-libx265 --enable-libxvid --enable-libx264 --enable-nonfree --enable-openssl --enable-libfdk_aac --enable-postproc --enable-small --enable-version3 --enable-libbluray --enable-libzmq --extra-libs=-ldl --prefix=/opt/ffmpeg --enable-libopenjpeg --enable-libkvazaar --enable-libaom --extra-libs=-lpthread --enable-libsrt --enable-nvenc --enable-cuda --enable-cuvid --enable-libnpp --extra-cflags='-I/opt/ffmpeg/include -I/opt/ffmpeg/include/ffnvcodec -I/usr/local/cuda/include/' --extra-ldflags='-L/opt/ffmpeg/lib -L/usr/local/cuda/lib64 -L/usr/local/cuda/lib32/'
  libavutil      56. 22.100 / 56. 22.100
  libavcodec     58. 35.100 / 58. 35.100
  libavformat    58. 20.100 / 58. 20.100
  libavdevice    58.  5.100 / 58.  5.100
  libavfilter     7. 40.101 /  7. 40.101
  libavresample   4.  0.  0 /  4.  0.  0
  libswscale      5.  3.100 /  5.  3.100
  libswresample   3.  3.100 /  3.  3.100
  libpostproc    55.  3.100 / 55.  3.100"
            };
#endif
            return await HttpHelper.Get<string>(url);
        }

        private async Task CancelClicked(FlowExecutorInfo worker)
        {
            if (await Confirm.Show("Labels.Cancel",
                Translater.Instant("Pages.Dashboard.Messages.CancelMesssage", worker)) == false)
                return; // rejected the confirm

#if (!DEMO)
            Blocker.Show();
            try
            {
                await HttpHelper.Delete($"{ApIUrl}/{worker.Uid}?libraryFileUid={worker.LibraryFile.Uid}");
                await Task.Delay(1000);
                await Refresh();
            }
            finally
            {
                Blocker.Hide();
            }
#endif
        }

        private async Task TogglePaused()
        {
            bool paused = SystemInfo.IsPaused;
            paused = !paused;
            await HttpHelper.Post($"/api/system/pause" + (paused == false ? "?resume=true" : ""));
            var systemInfoResult = await GetSystemInfo();
            if (systemInfoResult.Success)
            {
                SystemInfo = systemInfoResult.Data;
                this.lblPauseLabel = Translater.Instant(SystemInfo.IsPaused ? "Labels.Resume" : "Labels.Pause");
                this.StateHasChanged();
            }
        }
    }
}